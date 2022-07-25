using MongoDB.Bson.Serialization.Attributes;

namespace Logistics.Models
{
    public class DistanceCalculated
    {
        [BsonElement("distance")]
        public double Distance { get; set; }
    }
}
