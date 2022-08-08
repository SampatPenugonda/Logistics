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
        string[] cargoFieldsToProject = { CargoConstants.Received, CargoConstants.Location, CargoConstants.Destination, CargoConstants.Status };
        public CargoDal(IMongoClient mongoClient, ILogger<CargoDal> logger)
        {
            _mongoDbClient = mongoClient;
            _mongoDatabase = this._mongoDbClient.GetDatabase(SharedConstants.Database);
            cargoCollection = this._mongoDatabase.GetCollection<BsonDocument>(CargoConstants.CollectionName).WithWriteConcern(WriteConcern.Acknowledged).WithReadPreference(ReadPreference.SecondaryPreferred);
            _logger = logger;
        }

        public async Task<BsonDocument> AddCargo(string location, string destination)
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
                return cargo;
            }
            catch (MongoException mex)
            {
                this.processException(mex, "Error occured in adding cargo");
            }
            return null;        
        }

        public async Task<bool> UpdateCargo(string id)
        {
            var result = false;
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, ObjectId.Parse(id));
                var update = Builders<BsonDocument>.Update
                    .Set(CargoConstants.Status, CargoStatus.Delivered);

                var updateCargoResult = await cargoCollection.UpdateOneAsync(filter, update);
                result = updateCargoResult.IsAcknowledged && updateCargoResult.ModifiedCount == 1;
            }
            catch (MongoException ex)
            {
                this.processException(ex,$"Failed to update the cargo : {id} with status: {CargoStatus.Delivered}.Exception: ");
                
                result = false;
            }
            return result;

        }

        public async Task<BsonDocument> GetCargoById(string id)
        {
            var filter = new BsonDocument();
            filter[SharedConstants.UnderScoreId] = ObjectId.Parse(id);
            var project = Builders<BsonDocument>.Projection.Exclude(CargoConstants.schemaVersion);
            try
            {
                foreach(var cargoField in cargoFieldsToProject)
                {
                    project = project.Include(cargoField);
                }
                var cargosCursor = await cargoCollection.Find(filter).Project(project).ToListAsync();
                if (cargosCursor.Any())
                {
                    return cargosCursor.FirstOrDefault();
                }
            }
            catch (MongoException ex)
            {
                this.processException(ex, $"Failed to fetch the cargo by the id: {id}.Exception: ");
            }

            return null;
        }


        public async Task<BsonDocument> UpdateCargo(string id, string callsign)
        {
            
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, ObjectId.Parse(id));
                var update = Builders<BsonDocument>.Update
                    .Set(CargoConstants.Courier, callsign);

                return await cargoCollection.FindOneAndUpdateAsync(filter, update);
                
            }
            catch (MongoException ex)
            {
                this.processException(ex,$"Failed to assign courier : {callsign} to the cargo : {id}.Exception: ");
            }
            return null;

        }

        public async Task<BsonDocument> UnloadCargo(string id)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, ObjectId.Parse(id));
                var update = Builders<BsonDocument>.Update
                             .Unset(CargoConstants.Courier);

                 return await cargoCollection.FindOneAndUpdateAsync(filter, update);
            }
            catch (MongoException ex)
            {
                this.processException(ex,$"Failed to assign courier : {id} .Exception: ");
            }
            return null;
        }

        public async Task<BsonDocument> UpdateCargoLocation(string id, string location)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, ObjectId.Parse(id));
                var update = Builders<BsonDocument>.Update
                    .Set(CargoConstants.Location, location);

                return await cargoCollection.FindOneAndUpdateAsync(filter, update);
            }
            catch (MongoException ex)
            {
                this.processException(ex,$"Failed to assign courier : {id} to the cargo : {id}.Exception: ");
            }
            return null;
        }

        public async Task<List<BsonDocument>> GetCargos(string location)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(CargoConstants.Status, CargoStatus.InProcess) &
                         builder.Eq(CargoConstants.Location, location);
            var project = Builders<BsonDocument>.Projection.Include(cargoFieldsToProject[0])
                .Include(cargoFieldsToProject[1])
                .Include(cargoFieldsToProject[2])
                .Include(cargoFieldsToProject[3])
                ;
            var excludeProjection = Builders<BsonDocument>.Projection.Exclude(CargoConstants.schemaVersion);

            try
            {
                // Created index with status, location and courier -> db.cargos.createIndex({status:1,location:1})
                var cargoBsonDocs = await cargoCollection.Find(filter).Project(project).Project(excludeProjection).ToListAsync();
                return cargoBsonDocs;
            }
            catch (MongoException ex)
            {
                this.processException(ex,$"Failed to fetch the cargoes at the location: {location}.Exception: ");
            }
            return new List<BsonDocument>();
        }
        public string GetLastError()
        {
            return lastError;
        }
        private void processException(MongoException ex, string message)
        {
            lastError = $"{message} {ex.ToString()}";
            _logger.LogError(lastError);
        }
    }
}
