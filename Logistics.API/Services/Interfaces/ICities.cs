using Logistics.Models;

namespace Logistics.API.Services.Interfaces
{
    public interface ICities
    {
        Task<IEnumerable<City>> GetAll();
        Task<City> GetCityByName(string cityName);
        Task<List<City>> GetNeighbouringCities(string cityName, long count);
        string GetLastError();
    }
}
