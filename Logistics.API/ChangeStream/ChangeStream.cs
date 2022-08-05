
using Logistics.API.DAL;
using Logistics.Models;
using Logistics.Utills;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using System.Collections.Concurrent;

namespace Logistics.API.ChangeStream
{
    public class ChangeStream
    {
        private readonly IPlanesDal _planesDAL;
        private readonly ICargoDal _cargoDAL;
        private readonly ICitiesDal _citiesDAL;
        private readonly IMongoClient _mongoDbClient;
        private readonly IMongoCollection<BsonDocument> planeCollection;
        private readonly IMongoCollection<BsonDocument> cityCollection;
        private readonly IMongoCollection<BsonDocument> planeHistoryCollection;
        private readonly IMongoDatabase _mongoDatabase;
        public ChangeStream(IMongoClient mongoClient, IPlanesDal planesDal, ICargoDal cargoDal, ICitiesDal citiesDal)
        {
            _mongoDbClient = mongoClient;
            _mongoDatabase = this._mongoDbClient.GetDatabase(SharedConstants.Database);
            cityCollection = this._mongoDatabase.GetCollection<BsonDocument>(CityConstants.CollectionName);
            planeCollection = this._mongoDatabase.GetCollection<BsonDocument>(PlaneConstants.CollectionName);
            planeHistoryCollection = this._mongoDatabase.GetCollection<BsonDocument>(planeHistoryConstants.collectionName);

            this._planesDAL = planesDal;
            this._cargoDAL = cargoDal;
            this._citiesDAL = citiesDal;
        }
        public async Task Monitor()
        {
            //var _database = client.GetDatabase("logistics");
            //var planes = _database.GetCollection<Plane>(PlaneConstants.CollectionName);
            //var cities = _database.GetCollection<City>(CityConstants.CollectionName);

            try
            {

                //var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<Plane>>().Match("{ operationType: { $in: [ 'update'] }, 'updateDescription.updatedFields.landed' : { $exists: true } }");

                var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>()
                            .Match(x => x.OperationType == ChangeStreamOperationType.Update);
                var changeStreamOptions = new ChangeStreamOptions
                {
                    FullDocument = ChangeStreamFullDocumentOption.UpdateLookup
                };

                using (var cursor = await this.planeCollection.WatchAsync(pipeline, changeStreamOptions))
                {
                    await cursor.ForEachAsync(change =>
                    {
                        var doc = BsonSerializer.Deserialize<Plane>(change.FullDocument);
                        if (!string.IsNullOrWhiteSpace(doc.Landed) && !string.IsNullOrWhiteSpace(doc.PreviousLanded))
                        {
                            var travelledCitiesNames = new string[] { doc.Landed, doc.PreviousLanded };
                            var filter = Builders<BsonDocument>.Filter.In(CityConstants.CityName, travelledCitiesNames);
                            var travelledCities = this.cityCollection.Find(filter).ToList();
                            UpdateDoc(doc, travelledCities);
                        }
                        if (doc.Route != null)
                        {
                            this.PlaneLandedhistory(doc);
                        }
                    });
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Init()
        {
            new Thread(async () => await Monitor()).Start();
        }

        public static async void UpdateDoc(Plane doc, List<BsonDocument> travelledCitiesBson)
        {
            var travelledCities = new ConcurrentBag<City>();
            // Parallelizing the serialization to make it faster.
            Parallel.ForEach(travelledCitiesBson, cityDto =>
            {
                var cityModel = BsonSerializer.Deserialize<City>(cityDto);
                travelledCities.Add(cityModel);
            });
            if (travelledCities.Count != 2)
            {
                return;
            }
            var foundCities = travelledCities.ToArray();

            var travelledDistance = GetDistance(foundCities[0].Location, foundCities[1].Location);
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

        public async void PlaneLandedhistory(Plane document)
        {

            try
            {
                var planehistory = new BsonDocument()
            {
                { planeHistoryConstants.Route , new BsonArray{ document.Route.ToBsonDocument() } },
                { planeHistoryConstants.Callsign, document.Callsign },
                { planeHistoryConstants.LandedOn, DateTime.UtcNow },
                { planeHistoryConstants.Landed , document.Landed },
                { planeHistoryConstants.Heading , document.Heading }

            };
                await planeHistoryCollection.InsertOneAsync(planehistory);
            }
            catch (MongoException mex)
            {

                Console.WriteLine(mex);

            }
        }

        public static double ToRadian(double deg)
        {
            return deg * (Math.PI / 180);
        }
    }
}