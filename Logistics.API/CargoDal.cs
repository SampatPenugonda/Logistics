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

namespace Logistics.API
{
    public class CargoDal : ICargoDal
    {
        private readonly IMongoClient _mongoDbClient;
        private readonly IMongoCollection<Cargo> cargoCollection;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly ILogger<CargoDal> _logger;
        private string lastError = string.Empty;
        public CargoDal(IMongoClient mongoClient, ILogger<CargoDal> logger)
        {
            this._mongoDbClient = mongoClient;
            this._mongoDatabase = mongoClient.GetDatabase(SharedConstants.Database);
            this.cargoCollection = this._mongoDatabase.GetCollection<Cargo>(CargoConstants.CollectionName).WithWriteConcern(WriteConcern.Acknowledged).WithReadPreference(ReadPreference.SecondaryPreferred);
            this._logger = logger;
        }

        public async Task<Cargo> AddCargo(string location, string destination)
        {
            
            var cargo = new Cargo
            {
                Received = DateTime.UtcNow,
                Location = location,
                Destination = destination,
                Status = CargoStatus.InProcess,
                SchemaVersion = CargoConstants.schemaVersion
            };


            await this.cargoCollection.InsertOneAsync(cargo);
            return cargo;
        }

        public async Task<bool> UpdateCargo(string id)
        {
            var result = false;
            try
            {
                var filter = Builders<Cargo>.Filter.Eq(cargo => cargo.Id, id);
                var update = Builders<Cargo>.Update
                    .Set(cargo => cargo.Status, CargoStatus.Delivered);

                var updateCargoResult = await this.cargoCollection.UpdateOneAsync(filter, update);
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
                var cursor = await this.cargoCollection.FindAsync(filter);
                var cargos = cursor.ToList();
                if (cargos.Any())
                {
                    var cargoModel = cargos.FirstOrDefault();
                    return cargoModel;
                }
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to fetch the cargo by the id: {id}.Exception: {ex.ToString()}";
                this._logger.LogError(lastError);
            }

            return null;
        }


        public async Task<bool> UpdateCargo(string id, string callsign)
        {
            var result = false;
            try
            {

                var filter = Builders<Cargo>.Filter.Eq(cargo => cargo.Id, id);
                var update = Builders<Cargo>.Update
                    .Set(cargo => cargo.Courier, callsign);

                var updatedCargoResult = await this.cargoCollection.UpdateOneAsync(filter, update);

                result = updatedCargoResult.IsAcknowledged;

            }
            catch (MongoException ex)
            {
                lastError = $"Failed to assign courier : {callsign} to the cargo : {id}.Exception: {ex.ToString()}";
                this._logger.LogError(lastError);
                result = false;
            }
            return result;

        }

        public async Task<Cargo> UnloadCargo(string id)
        {
            try
            {
                var filter = Builders<Cargo>.Filter.Eq(cargo => cargo.Id, id);
                var update = Builders<Cargo>.Update
                    .Set(cargo => cargo.Courier, null);

                return await cargoCollection.FindOneAndUpdateAsync(filter, update);
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to assign courier : {id} to the cargo : {id}.Exception: {ex.ToString()}";
                this._logger.LogError(lastError);
            }
            return null;
        }

        public async Task<Cargo> UpdateCargoLocation(string id, string location)
        {
            try
            {
                var filter = Builders<Cargo>.Filter.Eq(cargo => cargo.Id, id);
                var update = Builders<Cargo>.Update
                    .Set(cargo => cargo.Location, location);

                var result = await this.cargoCollection.FindOneAndUpdateAsync(filter, update);

                return result;
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
            var builder = Builders<Cargo>.Filter;
            var filter = builder.Eq("status", CargoStatus.InProcess) &
                         builder.Eq("location", location);

            try
            {
                // Created index with status, location and courier -> db.cargos.createIndex({status:1,location:1})
                var cursor = await this.cargoCollection.FindAsync(filter);
                var cargoDtos = cursor.ToList();

                // Parallelizing the serialization to make it faster.
                Parallel.ForEach(cargoDtos, cargoDto =>
                {
                    var cargoModel =cargoDto;
                    cargos.Add(cargoModel);
                });
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to fetch the cargoes at the location: {location}.Exception: {ex.ToString()}";
                this._logger.LogError(lastError);
            }

            return cargos.OrderBy(x => x.Id).ToList();
            
        }
    }
}
