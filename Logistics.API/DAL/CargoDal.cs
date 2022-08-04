using Logistics.Models;
using Logistics.Utills;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.API.DAL
{
    public class CargoDal : ICargoDal
    {
        private readonly IMongoClient _mongoDbClient;
        private readonly IMongoCollection<BsonDocument> cargoCollection;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly ILogger<CargoDal> _logger;
        private string lastError = string.Empty;
        public CargoDal(IMongoClient mongoClient, ILogger<CargoDal> logger)
        {
            _mongoDbClient = mongoClient;
            _mongoDatabase = this._mongoDbClient.GetDatabase(SharedConstants.Database);
            cargoCollection = this._mongoDatabase.GetCollection<BsonDocument>(CargoConstants.CollectionName).WithWriteConcern(WriteConcern.Acknowledged).WithReadPreference(ReadPreference.SecondaryPreferred);
            _logger = logger;
        }

        public async Task<Cargo> AddCargo(string location, string destination)
        {
            var cargo = new BsonDocument()
            {
                { CargoConstants.Received, DateTime.UtcNow },
                { CargoConstants.Location, location },
                { CargoConstants.Destination, destination },
                { CargoConstants.Status, CargoConstants.InProgress },
                { CargoConstants.schemaVersion, CargoConstants.schemaVersionValue }
            };
            try
            {
                await cargoCollection.InsertOneAsync(cargo);
            }
            catch (Exception ex)
            {

            }
            return this.FetchCargo(cargo);
        }

        public async Task<bool> UpdateCargo(string id)
        {
            var result = false;
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var update = Builders<BsonDocument>.Update
                    .Set(CargoConstants.Status, CargoStatus.Delivered);

                var updateCargoResult = await cargoCollection.UpdateOneAsync(filter, update);
                result = updateCargoResult.IsAcknowledged && updateCargoResult.ModifiedCount == 1;
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to update the cargo : {id} with status: {CargoStatus.Delivered}.Exception: {ex.ToString()}";
                //this.logger.LogError(lastError);
                result = false;
            }
            return result;

        }

        public async Task<Cargo> GetCargoById(string id)
        {
            var filter = new BsonDocument();

            filter[SharedConstants.UnderScoreId] = id;
            try
            {
                // Will use _id index
                var cursor = await cargoCollection.FindAsync(filter);
                var cargos = cursor.ToList();
                if (cargos.Any())
                {
                    var cargoModel = cargos.FirstOrDefault();
                    return this.FetchCargo(cargoModel);
                }
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to fetch the cargo by the id: {id}.Exception: {ex.ToString()}";
                _logger.LogError(lastError);
            }

            return null;
        }


        public async Task<bool> UpdateCargo(string id, string callsign)
        {
            var result = false;
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var update = Builders<BsonDocument>.Update
                    .Set(CargoConstants.Courier, callsign);

                var updatedCargoResult = await cargoCollection.UpdateOneAsync(filter, update);

                result = updatedCargoResult.IsAcknowledged;
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to assign courier : {callsign} to the cargo : {id}.Exception: {ex.ToString()}";
                _logger.LogError(lastError);
                result = false;
            }
            return result;

        }

        public async Task<Cargo> UnloadCargo(string id)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var update = Builders<BsonDocument>.Update
                             .Unset(CargoConstants.Courier);

                 var updResult = await cargoCollection.FindOneAndUpdateAsync(filter, update);
                 return this.FetchCargo(updResult);
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to assign courier : {id} to the cargo : {id}.Exception: {ex.ToString()}";
                _logger.LogError(lastError);
            }
            return null;
        }

        public async Task<Cargo> UpdateCargoLocation(string id, string location)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var update = Builders<BsonDocument>.Update
                    .Set(CargoConstants.Location, location);

                var updResult = await cargoCollection.FindOneAndUpdateAsync(filter, update);
                return this.FetchCargo(updResult);
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to assign courier : {id} to the cargo : {id}.Exception: {ex.ToString()}";
                throw;
            }
            return null;
        }

        public async Task<List<Cargo>> GetCargos(string location)
        {
            var cargos = new ConcurrentBag<Cargo>();
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CargoConstants.Status, CargoStatus.InProcess) &
                         builder.Eq(CargoConstants.Location, location);

            try
            {
                // Created index with status, location and courier -> db.cargos.createIndex({status:1,location:1})
                var cursor = await cargoCollection.FindAsync(filter);
                var cargoDtos = cursor.ToList();

                // Parallelizing the serialization to make it faster.
                Parallel.ForEach(cargoDtos, cargoDto =>
                {
                    var cargoModel = this.FetchCargo(cargoDto);
                    cargos.Add(cargoModel);
                });
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to fetch the cargoes at the location: {location}.Exception: {ex.ToString()}";
                _logger.LogError(lastError);
            }

            return cargos.OrderBy(x => x.Id).ToList();

        }
        private Cargo FetchCargo(BsonDocument cargoDto)
        {
            var cargoModel = BsonSerializer.Deserialize<Cargo>(cargoDto);
            return cargoModel;
        }
    }
}
