
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
            this.citiesCollection = this.mongoDatabase.GetCollection<BsonDocument>(CityConstants.CollectionName).WithWriteConcern(WriteConcern.Acknowledged).WithReadPreference(ReadPreference.SecondaryPreferred);
            _logger = logger;
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

        public async Task<dynamic> GetNeighbouringCities(string cityName, long count)
        {
            var collection = this.mongoDatabase.GetCollection<City>(CityConstants.CollectionName);

            var cities = await collection.Find(city => city.Name == cityName).ToListAsync();

            if (cities.Any())
            {
                var city = cities.First();
                var point = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(new GeoJson2DGeographicCoordinates(city.Location[0], city.Location[1]));

                var filter = Builders<City>.Filter.Near(x => x.Location, point, 100000000000, 0);
                var nearbyCities = await collection.Find(filter).ToListAsync();

                var selectedCities = nearbyCities.Take((int)count).ToList();

                return new
                {
                    Neighbors = selectedCities.Select(x => new 
                    {
                        Name = x.Name,
                        Country = x.Country,
                        Location = new double[] { x.Location[0], x.Location[1] }
                    })
                };
            }

            return new
            {
                Neighbors = new List<City> { }
            };


        }
        public string GetLastError()
        {
            return lastError;
        }
    }
}
