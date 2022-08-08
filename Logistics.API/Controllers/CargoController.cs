using Logistics.API.Services.Interfaces;
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
        private readonly ICargo _cargo;
        
        public CargoController(ICargo cargo, IPlanes planes, ICitiesDal citiesDAL, IMongoClient client)
        {
            this._cargo = cargo;
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
            try
            {
                var result = await _cargo.AddCargo(location, destination);
                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, this._cargo.GetLastError());
            }
        }
        /// <summary>
        /// Modify the cargo details with Cargo Id. 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPut("cargo/{id}/delivered")]
        public async Task<bool> UpdateCargo(string id)
        {
            try
            {
                var cargo = await _cargo.UpdateCargo(id);
                return cargo;
            }
            catch(Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the cargo location. 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="callsign"></param>
        /// <returns></returns>
        [HttpPut("cargo/{id}/courier/{callsign}")]
        public async Task<IActionResult> UpdateCargo(string id, string callsign)
        {
            try
            {
                var result = await _cargo.UpdateCargo(id, callsign);
                return new OkObjectResult(result);
            }
            catch (Exception)
            {
                return StatusCode(500, this._cargo.GetLastError());
            }
        }

        /// <summary>
        /// UnLoad / Delivery the cargo. 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("cargo/{id}/courier")]
        public async Task<IActionResult> UnloadCargo(string id)
        {
            try
            {
                var result = await _cargo.UnloadCargo(id);
                return new OkObjectResult(result);
            }
            catch (Exception)
            {
                return StatusCode(500, this._cargo.GetLastError());
            }
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
            try
            {
                var result = await _cargo.UpdateCargoLocation(id, location);
                return new OkObjectResult(result);
            }
            catch (Exception)
            {
                return StatusCode(500, this._cargo.GetLastError());
            }
        }
        /// <summary>
        /// Retunrs all cargos based on the location. 
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        [HttpGet("cargo/location/{location}")]
        public async Task<ActionResult> GetCargos(string location)
        {
            try
            {
                var result = await _cargo.GetCargos(location);
                return new OkObjectResult(result);
            }
            catch (Exception)
            {
                return StatusCode(500, this._cargo.GetLastError());
            }
        }
    }
}
