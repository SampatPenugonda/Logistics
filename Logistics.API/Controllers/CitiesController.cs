
using Logistics.API.Services.Interfaces;
using Logistics.Models;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.API.Controllers
{
    [Route("/")]
    [ApiController]
    public class CitiesController : ControllerBase
    {
        private readonly ICities _city;
        
        //private readonly ILogger logger;    
        public CitiesController(ICities citiesService)
        {
            this._city = citiesService;
        }
        /// <summary>
        /// Fetches All Cities
        /// </summary>
        /// <returns></returns>
        [HttpGet("cities")]
        public async Task<IActionResult> GetCities()
        {
            try
            {
                var cities = await _city.GetAll();
                return new OkObjectResult(cities.ToList());
            }
            catch (Exception)
            {
                return StatusCode(500, this._city.GetLastError());
            }
        }
        /// <summary>
        /// Get City Based on the name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet("cities/{name}")]
        public async Task<dynamic> GetCity(string name)
        {
            try
            {
                var results = await _city.GetCityByName(name);
                return new OkObjectResult(results);
            }
            catch (Exception)
            {
                return StatusCode(500, this._city.GetLastError());
            }
        }
        /// <summary>
        /// Get Neighbouring Cities based on the Count & City Name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpGet("cities/{name}/neighbors/{count}")]
        public async Task<IActionResult> GetNeighbouringCities(string name, long count)
        {
            try
            {
                var results = await _city.GetNeighbouringCities(name, count);
                return new OkObjectResult(new { neighbors = results });
            }
            catch (Exception)
            {
                return StatusCode(500, this._city.GetLastError());
            }
        }



    }
}
