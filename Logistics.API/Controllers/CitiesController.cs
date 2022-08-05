
using Logistics.Models;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.API.Controllers
{
    [Route("/")]
    [ApiController]
    public class CitiesController : ControllerBase
    {
        private readonly ICitiesDal _citiesDAL;
        
        //private readonly ILogger logger;    
        public CitiesController(ICitiesDal citiesDAL)
        {
            this._citiesDAL = citiesDAL;
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
                var cities = await _citiesDAL.GetAll();
                return new OkObjectResult(cities.ToList());
            }
            catch (Exception)
            {
                return StatusCode(500, this._citiesDAL.GetLastError());
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
                var results = await _citiesDAL.GetCityByName(name);
                return new OkObjectResult(results);
            }
            catch (Exception)
            {
                return StatusCode(500, this._citiesDAL.GetLastError());
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
                var results = await _citiesDAL.GetNeighbouringCities(name, count);
                return new OkObjectResult(new { neighbors = results });
            }
            catch (Exception)
            {
                return StatusCode(500, this._citiesDAL.GetLastError());
            }
        }



    }
}
