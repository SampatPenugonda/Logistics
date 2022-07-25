using Logistics.Models;
using Logistics.Utills;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Logistics.API.ConfigureIndexes
{
    public static class LogisticsIndex
    {
        /// <summary>
        /// Creates Indexes on the defined collections for required fields.
        /// </summary>
        /// <param name="mongoClient"></param>
        /// <returns></returns>
        public static async Task createIndexes(MongoClient mongoClient)
        {
            var logisticsDatabase = mongoClient.GetDatabase(SharedConstants.Database);
            var citiesCollection = logisticsDatabase.GetCollection<City>(CityConstants.CollectionName);

            // Defining GeoSphere 2d Index on City Collection
            var indexKeysDefinition = Builders<City>.IndexKeys.Geo2DSphere(City => City.Location);
            await citiesCollection.Indexes.CreateOneAsync(new CreateIndexModel<City>(indexKeysDefinition));

            // Defining GeoSphere 2d Index on Plane Collection
            var planeCollection = logisticsDatabase.GetCollection<Plane>(PlaneConstants.CollectionName);
            var indexKeysDefinitionPlane = Builders<Plane>.IndexKeys.Geo2DSphere(Plane => Plane.CurrentLocation);
            await planeCollection.Indexes.CreateOneAsync(new CreateIndexModel<Plane>(indexKeysDefinitionPlane));
        }

    
    }
}
