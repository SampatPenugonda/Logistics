using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.Models
{
    public class PlaneHistory
    {
        [BsonElement("_id")]
        public string Callsign { get; set; }
        public double Heading { get; set; }
        [BsonElement("route")]
        public string[] Route { get; set; }
        [BsonElement("landed")]
        public string Landed { get; set; }
        [BsonElement("LandedOn")]
        public DateTime LandedOn { get; set; }

        [BsonElement("schemaversion")]
        public string SchemaVersion { get; set; }
    }
}
