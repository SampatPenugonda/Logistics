using Logistics.API.Services.Interfaces;
using Logistics.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace Logistics.API.Services
{
    public class CargoService : ICargo
    {
        private readonly ICargoDal _cargoDal;
        private readonly ILogger<CargoService> _logger;
        string lastError;
        public CargoService(ICargoDal cargoDal, ILogger<CargoService> logger)
        {
            _cargoDal = cargoDal;
            _logger = logger;   
        }

        public async Task<Cargo> AddCargo(string location, string destination)
        {
            try
            {
                var result = await _cargoDal.AddCargo(location, destination);
                return this.FetchCargo(result);
            }
            catch (Exception ex)
            {
                this.processException(ex, "Error in Adding Cargo");
            }
            return null;
        }

        public async Task<Cargo> GetCargoById(string id)
        {
            try
            {
                var cargoDoc = await _cargoDal.GetCargoById(id);
                return this.FetchCargo(cargoDoc);
            }
            catch(Exception ex)
            {
                this.processException(ex, $"Error in fetching Cargo by id {id} ");
            }
            return null;
        }

        public async Task<List<Cargo>> GetCargos(string location)
        {
            var cargos = new ConcurrentBag<Cargo>();
            try
            {
                var cargosDocs = await _cargoDal.GetCargos(location);
                Parallel.ForEach(cargosDocs, cargoDto =>
                {
                    var cargoModel = this.FetchCargo(cargoDto);
                    cargos.Add(cargoModel);
                });
            }
            catch (Exception ex)
            {
                this.processException(ex, "Error in GetCargos");
            }
            return cargos.ToList();
        }

        public string GetLastError()
        {
            return this.lastError;
        }

        public async Task<Cargo> UnloadCargo(string id)
        {
            try
            {
                var cargoDoc = await _cargoDal.UnloadCargo(id);
                return this.FetchCargo(cargoDoc);
            }
            catch (Exception ex)
            {
                this.processException(ex, $"Error in unload cargo {id}");
                throw;
            }
        }

        public async Task<bool> UpdateCargo(string id)
        {
            try
            {
                var result = await _cargoDal.UpdateCargo(id);
                return result;
            }
            catch ( Exception ex)
            {
                this.processException(ex, $"Error in updating cargo {id}");
                throw;
            }
        }

        public async Task<Cargo> UpdateCargo(string id, string callsign)
        {
            try
            {
                var result = await _cargoDal.UpdateCargo(id,callsign);
                return this.FetchCargo(result);
            }
            catch (Exception ex)
            {
                this.processException(ex, $"Error in updating cargo {id}");
                throw;
            }
        }

        public async Task<Cargo> UpdateCargoLocation(string id, string location)
        {
            try
            {
                var result = await _cargoDal.UpdateCargoLocation(id, location);
                return this.FetchCargo(result);
            }
            catch (Exception ex)
            {
                this.processException(ex, $"Error in updating cargo {id}");
                throw;
            }
        }
        private Cargo FetchCargo(BsonDocument cargoDto)
        {
            var cargoModel = BsonSerializer.Deserialize<Cargo>(cargoDto);
            return cargoModel;
        }
        private void processException(Exception ex, string message)
        {
            lastError = $"{message} {ex.ToString()}";
            _logger.LogError(lastError);
        }
    }
}
