using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Logistics.API.Controllers
{
    [Route("/")]
    [ApiController]
    public class PlanesController : ControllerBase
    {
        private readonly IPlanesDal _planeDAL;
        private readonly ICitiesDal _citiesDAL;
        //
        //private readonly ILogger logger;    
        public PlanesController(IPlanesDal planeDal, ICitiesDal citiesDAL)
        {
            this._planeDAL = planeDal;
            this._citiesDAL = citiesDAL;
        }

        /// <summary>
        /// Fetches all Planes
        /// </summary>
        /// <returns></returns>
        [HttpGet("planes")]
        public async Task<IActionResult> GetCities()
        {
            var planes = await _planeDAL.GetPlanes();
            if (planes.ToList().Count > 0)
            {
                return new OkObjectResult(planes);
            }
            else
            {
                return StatusCode(404);
            }
        }
        /// <summary>
        /// Fetches Plane Details based on the CallSign or Id
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        [HttpGet("planes/{callsign}")]
        public async Task<IActionResult> GetPlane(string callsign)
        {
            var plane = await _planeDAL.GetPlane(callsign);
            return plane != null ? new OkObjectResult(plane) : StatusCode(404);
        }

        /// <summary>
        /// Moves the plane across the locations
        /// </summary>
        /// <param name="id"></param>
        /// <param name="location"></param>
        /// <param name="heading"></param>
        /// <returns></returns>
        [HttpPut("planes/{id}/location/{location}/{heading}")]
        public async Task<IActionResult> MovePlaneLocation(string id, string location, int heading)
        {
            var result = await _planeDAL.MovePlaneLocation(id, location, heading);
            if (result == null)
            {
                return new BadRequestObjectResult(this._planeDAL.GetLastError());
            }
            return new JsonResult(result);

        }
        /// <summary>
        /// Updates the Plane location, based on the heading & City
        /// </summary>
        /// <param name="id"></param>
        /// <param name="location"></param>
        /// <param name="heading"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        [HttpPut("planes/{id}/location/{location}/{heading}/{city}")]
        public async Task<IActionResult> UpdateLandPlaneLocation(string id, [FromRoute] string location, int heading, string city)
        {
            if (string.IsNullOrEmpty(location))
            {
                return new BadRequestObjectResult("Location information is invalid");
            }
            var locations = location.Split(',');
            if (locations.Count() != 2)
            {
                return new BadRequestObjectResult("Location information is invalid");
            }
            var cityObtained = await this._citiesDAL.GetCityByName(city);
            if (cityObtained == null)
            {
                return new BadRequestObjectResult("Found invalid city");
            }

            var result = await _planeDAL.UpdateLandPlaneLocation(id, location, heading, city);
            if (result == null)
            {
                return new BadRequestObjectResult(this._planeDAL.GetLastError());
            }
            return new JsonResult(result);
        }

        [HttpPut("planes/{id}/route/{city}")]
        public async Task<IActionResult> AddDestination(string id, string city)
        {
            var cityObtained = await this._citiesDAL.GetCityByName(city);
            if (cityObtained == null)
            {
                return new BadRequestObjectResult("Found invalid city");
            }

            var result = await this._planeDAL.AddDestination(id, city);
            if (!result)
            {
                return new BadRequestObjectResult(this._planeDAL.GetLastError());
            }
            return new JsonResult(result);

        }
        /// <summary>
        /// Modifies the Destination
        /// </summary>
        /// <param name="id"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        [HttpPost("planes/{id}/route/{city}")]
        public async Task<IActionResult> UpdateDestination(string id, string city)
        {
            var cityObtained = await this._citiesDAL.GetCityByName(city);
            if (cityObtained == null)
            {
                return new BadRequestObjectResult("Found invalid city");
            }

            var result = await this._planeDAL.UpdateDestination(id, city);
            if (!result)
            {
                return new BadRequestObjectResult(this._citiesDAL.GetLastError());
            }

            return new JsonResult(result);

        }
        /// <summary>
        /// Deletes the city from the route, When it get landed.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("planes/{id}/route/destination")]
        public async Task<IActionResult> RemoveDestination(string id)
        {
            var result = await this._planeDAL.RemoveDestination(id);
            if (!result)
            {
                return new BadRequestObjectResult(this._planeDAL.GetLastError());
            }

            return new JsonResult(result);
        }

    }
}
