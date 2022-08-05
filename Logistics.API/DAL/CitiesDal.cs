using Logistics.Models;
using Logistics.Utills;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.API.DAL
{
    public class CitiesDal : ICitiesDal
    {
        private readonly IMongoClient _mongoDbClient;
        private readonly IMongoCollection<BsonDocument> citiesCollection;
        private readonly IMongoDatabase mongoDatabase;
        private readonly ILogger<CitiesDal> _logger;
        private string lastError = string.Empty;
        public CitiesDal(IMongoClient mongoDbClient, ILogger<CitiesDal> logger)
        {
            _mongoDbClient = mongoDbClient;
            mongoDatabase = mongoDbClient.GetDatabase(SharedConstants.Database);
            citiesCollection = mongoDatabase.GetCollection<BsonDocument>(CityConstants.CollectionName).WithWriteConcern(WriteConcern.Acknowledged).WithReadPreference(ReadPreference.SecondaryPreferred);
            _logger = logger;
        }
        public async Task<IEnumerable<City>> GetAll()
        {
            var cities = new ConcurrentBag<City>();
            try
            {
                var sort = Builders<BsonDocument>.Sort.Ascending(CityConstants.UnderScoreId);
                var findOptions = new FindOptions<BsonDocument, BsonDocument>()
                {
                    // Sort is to display the city names in order in the front end
                    Sort = sort
                };
                // Will use _id index
                var cityDtosCursor = await citiesCollection.FindAsync(new BsonDocument(), findOptions);
                var cityDtos = cityDtosCursor.ToList();

                // Parallelizing the serialization to make it faster.
                Parallel.ForEach(cityDtos, cityDto =>
                {
                    var cityModel = BsonSerializer.Deserialize<City>(cityDto);
                    cities.Add(cityModel);
                });
            }
            catch(MongoException mex)
            {
                _logger.LogError($"Failed to fetch the cities Exception: {mex.ToString()}");
            }

            return cities.ToList();
        }

        public async Task<City> GetCityByName(string cityName)
        {
            var filter = new BsonDocument();

            filter[SharedConstants.UnderScoreId] = cityName;
            try
            {
                // Will use _id index
                var cursor = await citiesCollection.FindAsync(filter);
                var cities = cursor.ToList();
                if (cities.Any())
                {
                    var cityModel = BsonSerializer.Deserialize<City>(cities.FirstOrDefault());
                    return cityModel;
                }

            }
            catch (MongoException ex)
            {
                lastError = $"Failed to fetch the city by id: {cityName} Exception: {ex.ToString()}";
                _logger.LogError(lastError);
            }

            return null;
        }

        public async Task<List<City>> GetNeighbouringCities(string cityName, long count)
        {
            List<City> neighBourCities = new List<City>();
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, cityName);
                var cities = await this.citiesCollection.Find(filter).ToListAsync();

                if (cities.Any())
                {
                    var city = this.FetchCity(cities.First());
                    var point = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(city.Location[0], city.Location[1]));

                    var nearFilter = Builders<BsonDocument>.Filter.Near(CityConstants.location, point, 100000000000, 0);
                    var nearbyCities = await this.citiesCollection.Find(nearFilter).ToListAsync();

                    var selectedCities = nearbyCities.Take((int)count).ToList();

                    foreach (var cityDto in selectedCities)
                    {
                        var cityModel = BsonSerializer.Deserialize<City>(cityDto);
                        cityModel.Location = new double[] { cityModel.Location[0], cityModel.Location[1] };
                        neighBourCities.Add(cityModel);
                    }
                }
                return neighBourCities;
            }
            catch (MongoException mex)
            {
                lastError = $"Failed to fetch the neighboring cities: {cityName} Exception: {mex.ToString()}";
                _logger.LogError(lastError);
                throw;
            }
        }
        public string GetLastError()
        {
            return lastError;
        }
        private City FetchCity(BsonDocument planeDto)
        {
            try
            {
                var cityModel = BsonSerializer.Deserialize<City>(planeDto);
                return cityModel;
            }
            catch (Exception ex)
            {
                lastError = $"Failed to fetch the city from bson document {ex.ToString()}";
                _logger.LogError(lastError);
                throw;
            }
        }
    }
}
