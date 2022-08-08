using Logistics.Models;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.API
{
    public interface ICitiesDal
    {
        Task<IEnumerable<BsonDocument>> GetAll();
        Task<BsonDocument> GetCityByName(string cityName);
        Task<List<BsonDocument>> GetNeighbouringCities(string cityName, long count);
        string GetLastError();
    }
}
