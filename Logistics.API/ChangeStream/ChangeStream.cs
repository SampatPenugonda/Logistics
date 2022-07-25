
using Logistics.Models;
using Logistics.Utills;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Logistics.API.ChangeStream
{
	public static class ChangeStream
	{
		public static async Task Monitor(IMongoClient client)
		{
			var _database = client.GetDatabase("logistics");
			var planes = _database.GetCollection<Plane>(PlaneConstants.CollectionName);
			var cities = _database.GetCollection<City>(CityConstants.CollectionName);

			try
			{
				var options = new ChangeStreamOptions { FullDocument = ChangeStreamFullDocumentOption.UpdateLookup };
				var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<Plane>>().Match("{ operationType: { $in: [ 'update'] }, 'updateDescription.updatedFields.landed' : { $exists: true } }");

				using (var cursor = await planes.WatchAsync(pipeline, options))
				{
					await cursor.ForEachAsync(change =>
					{
						var doc = change.FullDocument;

						if (!string.IsNullOrWhiteSpace(doc.Landed) && !string.IsNullOrWhiteSpace(doc.PreviousLanded))
						{
							var travelledCitiesNames = new string[] { doc.Landed, doc.PreviousLanded };
							var filter = Builders<City>.Filter.In(city => city.Name, travelledCitiesNames);

							var travelledCities = cities.Find(filter).ToList();

							UpdateDoc(doc, travelledCities);
						}
					});
				}
			}

			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		public static void UpdateDoc(Plane doc, List<City> travelledCities)
		{
			if (travelledCities.Count != 2)
			{
				return;
			}

			var travelledDistance = GetDistance(travelledCities[0].Location, travelledCities[1].Location);
			var timeTaken = (DateTime.UtcNow - doc.LandedOn).TotalMinutes;


			// Check if maintenance is required
			double distanceTravelledSinceLastMaintenance = 0;
			bool maintenanceRequired = false;

			if (doc.Statistics?.MaintenanceRequired ?? false == false)
			{
				distanceTravelledSinceLastMaintenance = doc.Statistics?.DistanceTravelledSinceLastMaintenanceInMiles ?? 0 + travelledDistance;
				maintenanceRequired = distanceTravelledSinceLastMaintenance > 50000;
			}

			doc.Statistics.DistanceTravelledSinceLastMaintenanceInMiles = distanceTravelledSinceLastMaintenance;
			doc.Statistics.MaintenanceRequired = maintenanceRequired;

			doc.Statistics.TotalDistanceTravelledInMiles = doc.Statistics.TotalDistanceTravelledInMiles + travelledDistance;
			doc.Statistics.AirtimeInMinutes = doc.Statistics.AirtimeInMinutes + timeTaken;

		}

		// Get distance between two points in miles using the Haversine formula on the earth
		public static double GetDistance(double[] city1, double[] city2)
		{
			var lat1 = city1[0];
			var lon1 = city1[1];
			var lat2 = city2[0];
			var lon2 = city2[1];

			var R = 3959.87433; // In miles
			var dLat = ToRadian(lat2 - lat1);
			var dLon = ToRadian(lon2 - lon1);
			var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadian(lat1)) * Math.Cos(ToRadian(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
			var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));

			var distance = R * c;

			return distance;
		}

		public static double ToRadian(double deg)
		{
			return deg * (Math.PI / 180);
		}
	}
}