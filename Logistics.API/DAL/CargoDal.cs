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
        private readonly IMongoCollection<BsonDocument> cargoCollection;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly ILogger<CargoDal> _logger;
        private string lastError = string.Empty;
        public CargoDal(IMongoClient mongoClient, ILogger<CargoDal> logger)
        {
            this._mongoDbClient = mongoClient;
            this._mongoDatabase = mongoClient.GetDatabase(SharedConstants.Database);
            var databaseWithWriteConcern = this._mongoDatabase.WithWriteConcern(WriteConcern.WMajority).WithReadConcern(ReadConcern.Majority);
            this.cargoCollection = databaseWithWriteConcern.GetCollection<BsonDocument>(CargoConstants.CollectionName);
            this._logger = logger;
        }

        public async Task<Cargo> AddCargo(string location, string destination)
        {
            var id = new ObjectId();
            var cargo = new BsonDocument()
            {
             {SharedConstants.UnderScoreId, id},
             {CargoConstants.Location,  location},
             {CargoConstants.Destination,  destination},
             {CargoConstants.Status, CargoConstants.InProgress},
             {CargoConstants.schemaVersion, CargoConstants.schemaVersion }
            };
            await this.cargoCollection.InsertOneAsync(cargo);
            var newCargo = await this.GetCargoById(cargo[SharedConstants.UnderScoreId].ToString());
            return newCargo;
        }

        public async Task<bool> UpdateCargo(string id)
        {
            var result = false;
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var update = Builders<BsonDocument>.Update
                    .Set(CargoConstants.Status, CargoStatus.Delivered);

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
                    var cargoModel = this.FetchCargo(cargos.FirstOrDefault());
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

                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var update = Builders<BsonDocument>.Update
                    .Set(CargoConstants.Courier, callsign);

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

        public async Task<bool> UnloadCargo(string id)
        {
            var result = false;
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var update = Builders<BsonDocument>.Update
                             .Unset(CargoConstants.Courier);

                var unLoadCargoResult =  await cargoCollection.UpdateOneAsync(filter, update);
                result = unLoadCargoResult.IsAcknowledged;
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to assign courier : {id} to the cargo : {id}.Exception: {ex.ToString()}";
                this._logger.LogError(lastError);
            }
            return result;
        }

        public async Task<bool> UpdateCargoLocation(string id, string location)
        {
            var result = false;
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var update = Builders<BsonDocument>.Update
                    .Set(CargoConstants.Location, location);

                var updatedCargoresult = await this.cargoCollection.UpdateOneAsync(filter, update);

                result = updatedCargoresult.IsAcknowledged;
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to assign courier : {id} to the cargo : {id}.Exception: {ex.ToString()}";
                throw;
            }
            return result;
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
                var cursor = await this.cargoCollection.FindAsync(filter);
                var cargoDtos = cursor.ToList();

                // Parallelizing the serialization to make it faster.
                Parallel.ForEach(cargoDtos, cargoDto =>
                {
                    var cargoModel =this.FetchCargo(cargoDto);
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
        private Cargo FetchCargo(BsonDocument cargoDto)
        {
            var cargoModel = BsonSerializer.Deserialize<Cargo>(cargoDto);
            return cargoModel;
        }
    }
}
