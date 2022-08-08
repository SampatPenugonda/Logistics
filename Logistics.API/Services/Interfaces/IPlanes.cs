using Logistics.Models;

namespace Logistics.API.Services.Interfaces
{
    public interface IPlanes
    {
        Task<IEnumerable<Plane>> GetPlanes();
        Task<Plane> GetPlane(string callSign);
        Task<Plane> MovePlaneLocation(string id, string location, int heading);
        Task<bool> AddDestination(string id, string city);
        Task<bool> UpdateDestination(string id, string city);
        Task<bool> RemoveDestination(string id);
        Task<Plane> UpdateLandPlaneLocation(string id, string location, int heading, string city);
        string GetLastError();
    }
}
