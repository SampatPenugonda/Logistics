
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

namespace Logistics.API
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
            this._mongoDbClient = mongoDbClient;
            this.mongoDatabase = mongoDbClient.GetDatabase(SharedConstants.Database);
            var databaseWithWriteConcern = this.mongoDatabase.WithWriteConcern(WriteConcern.WMajority).WithReadConcern(ReadConcern.Majority);
            this.citiesCollection = databaseWithWriteConcern.GetCollection<BsonDocument>(CityConstants.CollectionName);
            this._logger = logger;
        }
        public async Task<IEnumerable<City>> GetAll()
        {
            var sort = Builders<BsonDocument>.Sort.Ascending(CityConstants.UnderScoreId);
            var findOptions = new FindOptions<BsonDocument, BsonDocument>()
            {
                // Sort is to display the city names in order in the front end
                Sort = sort
            };
            // Will use _id index
            var cityDtosCursor = await this.citiesCollection.FindAsync(new BsonDocument(), findOptions);
            var cityDtos = cityDtosCursor.ToList();
            var cities = new ConcurrentBag<City>();
            // Parallelizing the serialization to make it faster.
            Parallel.ForEach(cityDtos, cityDto =>
            {
                var cityModel = BsonSerializer.Deserialize<City>(cityDto);
                cities.Add(cityModel);
            });

            return cities.ToList();
        }

        public async Task<City> GetCityByName(string cityName)
        {
            var filter = new BsonDocument();

            filter[SharedConstants.UnderScoreId] = cityName;
            try
            {
                // Will use _id index
                var cursor = await this.citiesCollection.FindAsync(filter);
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
                this._logger.LogError(lastError);
            }

            return null;
        }

        public async Task<List<City>> GetNeighbouringCities(string cityName, long count)
        {
            var filter = Builders<BsonDocument>.Filter.Eq(CityConstants.UnderScoreId, cityName);
            List<City> neighBourCities = new List<City>();

            var cities = await this.citiesCollection.Find(filter).ToListAsync();

            if (cities.Any())
            {
                var city = this.FetchCity(cities.First());
                var point = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(city.Location[0], city.Location[1]));

                var filterNearCities = Builders<BsonDocument>.Filter.Near(CityConstants.Location, point, 100000000000, 0);
                var nearbyCities = await this.citiesCollection.Find(filter).ToListAsync();

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
        public string GetLastError()
        {
            return lastError;
        }
        private City FetchCity(BsonDocument cityDto)
        {
            var cityModel = BsonSerializer.Deserialize<City>(cityDto);
            return cityModel;
        }
    }
}
