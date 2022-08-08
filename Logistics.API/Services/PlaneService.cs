using Logistics.API.Services.Interfaces;
using Logistics.Models;
using Logistics.Utills;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace Logistics.API.Services
{
    public class PlaneService : IPlanes
    {
        private readonly ILogger<PlaneService> _logger;
        private readonly IPlanesDal _planeDAL;

        string lastError;
        public PlaneService(IPlanesDal planeDAL, ILogger<PlaneService> logger)
        {
            _planeDAL = planeDAL;
            _logger = logger;
        }
        public async Task<bool> AddDestination(string id, string city)
        {
            try
            {
                return await _planeDAL.AddDestination(id, city);
            }
            catch (Exception ex)
            {
                this.processException(ex, "Error in Add Destination");
                return false;
            }
        }

        public string GetLastError()
        {
            return lastError;
        }

        public async Task<Plane> GetPlane(string callSign)
        {
            try
            {
                var plane = await _planeDAL.GetPlane(callSign);
                return this.FetchPlane(plane);
            }
            catch (Exception ex)
            {
                this.processException(ex, "Failed to fetch the planes Exception");
                _logger.LogError(lastError);
            }
            return null;
        }


        public async Task<IEnumerable<Plane>> GetPlanes()
        {
            var planes = new ConcurrentBag<Plane>();
            try
            {
                var planesBsonData = await _planeDAL.GetPlanes();

                // Parallelizing the serialization to make it faster.
                Parallel.ForEach(planesBsonData, planeDto =>
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
            try
            {
                var planeInfo = await _planeDAL.MovePlaneLocation(id, location, heading);
                return this.FetchPlane(planeInfo);
            }
            catch (Exception ex)
            {
                this.processException(ex, "Error Occured in Moving Plane Locaiton");
                return null;
            }
        }

        public async Task<bool> RemoveDestination(string id)
        {
            try
            {
                return await _planeDAL.RemoveDestination(id);
            }
            catch (Exception ex)
            {
                this.processException(ex, "Error Occured in removing destination");
                return false;
            }
        }

        public async Task<bool> UpdateDestination(string id, string city)
        {
            try
            {
                return await _planeDAL.UpdateDestination(id, city);
            }
            catch (Exception ex)
            {
                this.processException(ex, "Error Occured in updating destination");
                return false;
            }
        }

        public async Task<Plane> UpdateLandPlaneLocation(string id, string location, int heading, string city)
        {
            try
            {
                var modifiedPlaneInfo = await _planeDAL.UpdateLandPlaneLocation(id, location, heading, city);
                return this.FetchPlane(modifiedPlaneInfo);
            }
            catch (Exception ex)
            {
                this.processException(ex, "Error Occured in Updating Land & Plane Location");
                return null;
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
                this.processException(ex, "Failed to fetch Plane data from bson document");
            }
            return null;
        }

        private void processException(Exception ex, string message)
        {
            lastError = $"{message} {ex.ToString()}";
            _logger.LogError(lastError);
        }
    }
}
