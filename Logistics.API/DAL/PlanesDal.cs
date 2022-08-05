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
    public class PlanesDal : IPlanesDal
    {
        private readonly IMongoClient _mongoDbClient;
        private readonly IMongoCollection<BsonDocument> planesCollection;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly ILogger _logger;
        private string lastError = string.Empty;
        private bool isTimeSet = false;
        string[] planeFieldsToProject = { SharedConstants.UnderScoreId, PlaneConstants.CurrentLocation, PlaneConstants.Heading, PlaneConstants.Route, PlaneConstants.Landed, PlaneConstants.PreviousLanded };
        public PlanesDal(IMongoClient mongoClient, ILogger<PlanesDal> logger)
        {
            _mongoDbClient = mongoClient;
            _mongoDatabase = mongoClient.GetDatabase(SharedConstants.Database);
            var databaseWithWriteConcern = this._mongoDatabase.WithWriteConcern(WriteConcern.Acknowledged).WithReadPreference(ReadPreference.SecondaryPreferred);
            this.planesCollection = databaseWithWriteConcern.GetCollection<BsonDocument>(PlaneConstants.CollectionName);
            _logger = logger;
        }

        public async Task<Plane> GetPlane(string callSign)
        {
            var filter = new BsonDocument();
            filter[SharedConstants.UnderScoreId] = callSign;
            var project = Builders<BsonDocument>.Projection.Include(SharedConstants.UnderScoreId);
            try
            {
                foreach (var planeField in planeFieldsToProject)
                {
                    project = project.Include(planeField);
                }
                // Will use _id index
                var planes = await planesCollection.Find(filter).Project(project).ToListAsync();
                if (planes.Any())
                {
                    var planeModel = this.FetchPlane(planes.FirstOrDefault());
                    return planeModel;
                }

            }
            catch (MongoException ex)
            {
                lastError = $"Failed to fetch the plane by id: {callSign} Exception: {ex.ToString()}";
                _logger.LogError(lastError);
            }

            return null;
        }

        public async Task<IEnumerable<Plane>> GetPlanes()
        {
            var planes = new ConcurrentBag<Plane>();
            try
            {
                var project = Builders<BsonDocument>.Projection.Include(SharedConstants.UnderScoreId);
                foreach (var planeField in planeFieldsToProject)
                {
                    project = project.Include(planeField);
                }
                var planeDtosCursor = await planesCollection.Find(new BsonDocument()).Project(project).ToListAsync();


                // Parallelizing the serialization to make it faster.
                Parallel.ForEach(planeDtosCursor, planeDto =>
                {
                    var planeModel = this.FetchPlane(planeDto);
                    planes.Add(planeModel);
                });
            }
            catch (MongoException mex)
            {
                lastError = $"Failed to fetch the planes Exception: {mex.ToString()}";
                _logger.LogError(lastError);
            }

            return planes.ToList();
        }



        public async Task<Plane> MovePlaneLocation(string id, string location, int heading)
        {
            // var point = new GeoJson2DCoordinates(double.Parse(location.Split(',')[0]), double.Parse(location.Split(',')[1]));
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var planeResult = await planesCollection.Find(filter).FirstOrDefaultAsync();
                var plane = this.FetchPlane(planeResult);

                double travelledDistance = DistanceTo(location.Split(",").Select(x => double.Parse(x)).ToArray(), id);
                double distanceTravelledSinceLastMaintenance = 0;
                bool maintenanceRequired = false;
                double totalSecond = 0;
                if (plane.PlaneStartedAt != default)
                {
                    totalSecond = CalculateTime(plane.PlaneStartedAt);
                }

                if (!plane.MaintenanceRequired)
                {
                    distanceTravelledSinceLastMaintenance = plane.DistanceTravelledSinceLastMaintenance + travelledDistance;
                    maintenanceRequired = distanceTravelledSinceLastMaintenance > 50000;
                }


                //var travelledDistance = DistanceTo(location.Split(",").Select(x => double.Parse(x)).ToArray(), plane.CurrentLocation);


                var update = Builders<BsonDocument>.Update
                   .Set(PlaneConstants.CurrentLocation, location.Split(",").Select(x => double.Parse(x)).ToArray())
                   .Set(PlaneConstants.Heading, heading)
                   .Set(PlaneConstants.distanceTravelledFromLastMaintainence, distanceTravelledSinceLastMaintenance)
                   .Set(PlaneConstants.MaintainenceRequired, maintenanceRequired)
                   .Set(PlaneConstants.totalDistanceTravelled, plane.TotalDistanceTravelled + travelledDistance)
                   .Set(PlaneConstants.travelledInSeconds, totalSecond);

                if (plane.PlaneStartedAt == default && !isTimeSet)
                {
                    update = Builders<BsonDocument>.Update
                   .Set(PlaneConstants.CurrentLocation, location.Split(",").Select(x => double.Parse(x)).ToArray())
                   .Set(PlaneConstants.Heading, heading)
                   .Set(PlaneConstants.distanceTravelledFromLastMaintainence, distanceTravelledSinceLastMaintenance)
                   .Set(PlaneConstants.MaintainenceRequired, maintenanceRequired)
                   .Set(PlaneConstants.totalDistanceTravelled, plane.TotalDistanceTravelled + travelledDistance)
                   .Set(PlaneConstants.travelledInSeconds, totalSecond)
                    .Set(PlaneConstants.planeStartedAt, DateTime.UtcNow);

                    isTimeSet = true;
                }
                var result = await planesCollection.FindOneAndUpdateAsync(filter, update);
                return this.FetchPlane(result);
            }
            catch (MongoException mex)
            {
                lastError = $"Failed to move the planes Exception: {mex.ToString()}";
                _logger.LogError(lastError);
            }
            return null;
            
        }

        private double DistanceTo(double[] currentLocation, string Id)
        {
            PipelineDefinition<Plane, DistanceCalculated> distanceQuery = new BsonDocument[]
                {
                new BsonDocument("$geoNear",
                new BsonDocument
                    {
                        { "near",
                new BsonDocument
                        {
                            { "type", "Point" },
                            { "coordinates",
                new BsonArray
                            {
                                currentLocation[0],
                                currentLocation[1]
                            } }
                        } },
                        { "distanceField", "distance" },
                        { "query",
                new BsonDocument(SharedConstants.UnderScoreId, Id) },
                        { "distanceMultiplier", 0.001 },
                        { "spherical", true }
                    }),
                new BsonDocument("$project",
                new BsonDocument
                    {
                        { "distance", 1 },
                        { "_id", 0 }
                    })
                }; 
            try
            {
                var data = this._mongoDatabase.GetCollection<Plane>(PlaneConstants.CollectionName).Aggregate(distanceQuery).FirstOrDefault();
                return data.Distance;

            }
            catch (MongoException mex)
            {
                lastError = $"Failed to fetch the calculate distance using geoNear Exception: {mex.ToString()}";
                _logger.LogError(lastError);
                return 0;
            }


        }



        private double CalculateTime(DateTime planeStartedAt)
        {
            var time = planeStartedAt.ToUniversalTime();
            var currentTime = DateTime.UtcNow;
            return (currentTime - time).TotalSeconds;
        }

        public async Task<bool> AddDestination(string id, string city)
        {
            var result = false;
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var update = Builders<BsonDocument>.Update
                .Set(PlaneConstants.Route, new string[] { city });

                var updatedPlaneResult = await planesCollection.UpdateOneAsync(filter, update);
                result = updatedPlaneResult.IsAcknowledged;
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to add plane route : {city} for the plane: {id}.Exception: {ex.ToString()}";
                //this.logger.LogError(lastError);
                result = false;
            }
            return result;
        }
        public string GetLastError()
        {
            return lastError;
        }

        public async Task<bool> UpdateDestination(string id, string city)

        {
            var result = false;
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var update = Builders<BsonDocument>.Update
                    .AddToSet(PlaneConstants.Route, city);
                var updatedPlaneResult = await planesCollection.UpdateOneAsync(filter, update);
                result = updatedPlaneResult.IsAcknowledged;
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to replace plane route : {city} for the plane: {id}.Exception: {ex.ToString()}";
                // this.logger.LogError(lastError);
                result = false;
            }
            return result;
        }

        public async Task<bool> RemoveDestination(string id)
        {
            var result = false;
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var project = Builders<BsonDocument>.Projection.Include(PlaneConstants.Route).Include(PlaneConstants.Heading);

                //var collection = _database.GetCollection<Plane>(PlaneConstants.CollectionName);
                var planeResult = await planesCollection.Find(filter).Project(project).FirstOrDefaultAsync();
                var plane = this.FetchPlane(planeResult);

                var previouslyLanded = string.Empty;

                if (plane?.Route?.Length > 0)
                {
                    previouslyLanded = plane.Route.First();
                }

                var update = Builders<BsonDocument>.Update
                                            .Set(PlaneConstants.PreviousLanded, previouslyLanded)
                                            .PopFirst(PlaneConstants.Route);

                var updResult = await planesCollection.UpdateOneAsync(filter, update);
                result = updResult.IsAcknowledged;
            }
            catch (MongoException ex)
            {
                lastError = $"Failed to remove the first route  for the plane: {id}.Exception: {ex.ToString()}";
                _logger.LogError(lastError);
                result = false;
            }
            return result;
        }

        public async Task<Plane> UpdateLandPlaneLocation(string id, string location, int heading, string city)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq(SharedConstants.UnderScoreId, id);
                var project = Builders<BsonDocument>.Projection.Include(PlaneConstants.CurrentLocation).Include(PlaneConstants.planeStartedAt).Include(PlaneConstants.Heading);
                var planeResult = await planesCollection.Find(filter).FirstOrDefaultAsync();
                var plane = this.FetchPlane(planeResult);
                var previousLocation = plane.CurrentLocation;
                var newLocation = location.Split(",").Select(x => double.Parse(x)).ToArray();


                bool maintenanceRequired = false;
                double totalSecond = CalculateTime(plane.PlaneStartedAt);


                var update = Builders<BsonDocument>.Update
                   .Set(PlaneConstants.CurrentLocation, location.Split(",").Select(x => double.Parse(x)).ToArray())
                   .Set(PlaneConstants.Heading, heading)
                   .Set(PlaneConstants.MaintainenceRequired, maintenanceRequired)
                   .Set(PlaneConstants.travelledInSeconds, totalSecond);
                var result = await planesCollection.FindOneAndUpdateAsync(filter, update);

                return this.FetchPlane(result);
            }
            catch (MongoException mex)
            {
                lastError = $"Failed to remove the first route  for the plane: {id}.Exception: {mex.ToString()}";
                _logger.LogError(lastError);
                throw;
            }
        }
        private Plane FetchPlane(BsonDocument planeDto)
        {
            try
            {
                var planeModel = BsonSerializer.Deserialize<Plane>(planeDto);
                planeModel.Heading = Convert.ToDouble(string.Format("{0:N2}", planeDto.GetValue(PlaneConstants.Heading).ToDouble()));
                return planeModel;
            }
            catch (Exception ex)
            {
                lastError = $"Failed to fetch Plane data from bson document {ex.ToString()}";
                _logger.LogError(lastError);
            }
            return null;
        }
    }


}
