using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;

namespace Logistics.Models
{
	public class City
	{
		[BsonElement("_id")]
		public string Name { get; set; }
		[BsonElement("country")]
		public string Country { get; set; }
		[BsonElement("position")]
		public double[] Location { get; set; }
	}
}
