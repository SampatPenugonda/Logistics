using Logistics.API.Services.Interfaces;
using Logistics.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using System.Collections.Concurrent;

namespace Logistics.API.Services
{
    public class CityService : ICities
    {
        private readonly ICitiesDal _citiesDal;
        private readonly ILogger<CityService> _logger;  
        string lastError;
        public CityService(ICitiesDal citiesDal, ILogger<CityService> logger)
        {
            _citiesDal = citiesDal; _logger = logger;
        }

        public async Task<IEnumerable<City>> GetAll()
        {
            var cities = new ConcurrentBag<City>();

            try
            {
                var cityBsonDocs = await _citiesDal.GetAll();

                // Parallelizing the serialization to make it faster.
                Parallel.ForEach(cityBsonDocs, cityDto =>
                {
                    var cityModel = this.FetchCity(cityDto);
                    cities.Add(cityModel);
                });
            }
            catch (Exception ex)
            {
                this.processException(ex, "Error in fetching cities");
            }
            return cities;
        }

        public async Task<City> GetCityByName(string cityName)
        {
            try
            {
                var cityBsonDoc = await _citiesDal.GetCityByName(cityName);
                return FetchCity(cityBsonDoc);
            }
            catch (Exception ex)
            {
                this.processException(ex, $"Error in fetching city by {cityName}");
            }
            return null;
        }

        public string GetLastError()
        {
            throw new NotImplementedException();
        }

        public async Task<List<City>> GetNeighbouringCities(string cityName, long count)
        {
            List<City> cities = new List<City>();
            try
            {
                var neighborCities = await _citiesDal.GetNeighbouringCities(cityName, count);
                foreach (var cityDto in neighborCities)
                {
                    var cityModel = BsonSerializer.Deserialize<City>(cityDto);
                    cityModel.Location = new double[] { cityModel.Location[0], cityModel.Location[1] };
                    cities.Add(cityModel);
                }
            }
            catch (Exception ex)
            {
                this.processException(ex, $"Error in processing neighbouring cities for {cityName}");
            }
            return cities;
        }
        private void processException(Exception ex, string message)
        {
            lastError = $"{message} {ex.ToString()}";
            _logger.LogError(lastError);
        }
        private City FetchCity(BsonDocument cityCargo)
        {
            var cargoModel = BsonSerializer.Deserialize<City>(cityCargo);
            return cargoModel;
        }
    }
}
