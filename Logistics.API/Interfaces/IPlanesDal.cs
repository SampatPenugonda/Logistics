using Logistics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.API
{
    public interface IPlanesDal
    {
        Task<IEnumerable<Plane>> GetPlanes();
        Task<Plane> GetPlane(string callSign);
        Task<bool> MovePlaneLocation(string id, string location, int heading);
        Task<bool> AddDestination(string id, string city);
        Task<bool> UpdateDestination(string id, string city);
        Task<bool> RemoveDestination(string id);
        Task<bool> UpdateLandPlaneLocation(string id, string location, int heading, string city);
        string GetLastError();
    }
}
