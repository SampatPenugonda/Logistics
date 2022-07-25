using Logistics.Models;
using Logistics.Utills;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Logistics.API.Controllers
{

    [Route("/")]
    [ApiController]
    public class CargoController : ControllerBase
    {
        private readonly ICargoDal _cargoDAL;
        private readonly ICitiesDal _citiesDAL;

        private readonly IPlanesDal _planesDAL;
        private readonly IMongoDatabase _database;

        public CargoController(ICargoDal cargoDAL, IPlanesDal planesDAL, ICitiesDal citiesDAL, IMongoClient client)
        {
            this._cargoDAL = cargoDAL;
            this._citiesDAL = citiesDAL;
            this._planesDAL = planesDAL;
            _database = client.GetDatabase("logistics");
        }

        /// <summary>
        /// Adds Cargo based on the location & designation
        /// </summary>
        /// <param name="location"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        [HttpPost("cargo/{location}/to/{destination}")]
        public async Task<IActionResult> AddCargo(string location, string destination)
        {
            var result = await _cargoDAL.AddCargo(location, destination);
            return new OkObjectResult(result);
        }
        /// <summary>
        /// Modify the cargo details with Cargo Id. 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPut("cargo/{id}/delivered")]
        public async Task<bool> UpdateCargo(string id)
        {
            var cargo = await _cargoDAL.UpdateCargo(id);
            return cargo;
        }

        /// <summary>
        /// Updates the cargo location. 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="callsign"></param>
        /// <returns></returns>
        [HttpPut("cargo/{id}/courier/{callsign}")]
        public async Task<bool> UpdateCargo(string id, string callsign)
        {
            var result = await _cargoDAL.UpdateCargo(id, callsign);
            return result;
        }

        /// <summary>
        /// UnLoad / Delivery the cargo. 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("cargo/{id}/courier")]
        public async Task<IActionResult> UnloadCargo(string id)
        {
            var result = await _cargoDAL.UnloadCargo(id);
            return new OkObjectResult(result);
        }
        /// <summary>
        /// Update the Cargo Location based on the Id, Location. 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpPut("cargo/{id}/location/{location}")]
        public async Task<IActionResult> UpdateCargoLocation(string id, string location)
        {
            var result = await _cargoDAL.UpdateCargoLocation(id, location);
            return new OkObjectResult(result);
        }
        /// <summary>
        /// Retunrs all cargos based on the location. 
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpGet("cargo/location/{location}")]
        public async Task<ActionResult> GetCargos(string location)
        {
            var result = await _cargoDAL.GetCargos(location);
            return new OkObjectResult(result);
        }
    }
}
