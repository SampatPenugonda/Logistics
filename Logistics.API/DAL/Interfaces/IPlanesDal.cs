using Logistics.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.API
{
    public interface IPlanesDal
    {
        Task<List<BsonDocument>> GetPlanes();
        Task<BsonDocument> GetPlane(string callSign);
        Task<BsonDocument> MovePlaneLocation(string id, string location, int heading);
        Task<bool> AddDestination(string id, string city);
        Task<bool> UpdateDestination(string id, string city);
        Task<bool> RemoveDestination(string id);
        Task<BsonDocument> UpdateLandPlaneLocation(string id, string location, int heading, string city);
        string GetLastError();
    }
}
