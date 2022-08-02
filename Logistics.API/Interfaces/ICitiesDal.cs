using Logistics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.API
{
    public interface ICitiesDal
    {
        Task<IEnumerable<City>> GetAll();
        Task<City> GetCityByName(string cityName);
        Task<List<City>> GetNeighbouringCities(string cityName, long count);
        string GetLastError();
    }
}
