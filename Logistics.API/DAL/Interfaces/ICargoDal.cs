using Logistics.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.API
{
    public interface ICargoDal
    {
        Task<BsonDocument> AddCargo(string location, string destination);
        Task<bool> UpdateCargo(string id);
        Task<BsonDocument> GetCargoById(string id);
        Task<BsonDocument> UpdateCargo(string id, string callsign);
        Task<BsonDocument> UnloadCargo(string id);
        Task<BsonDocument> UpdateCargoLocation(string id, string location);
        Task<List<BsonDocument>> GetCargos(string location);
        string GetLastError();
    }
}
