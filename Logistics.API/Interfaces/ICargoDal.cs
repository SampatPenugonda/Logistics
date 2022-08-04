using Logistics.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistics.API
{
    public interface ICargoDal
    {
        Task<Cargo> AddCargo(string location, string destination);
        Task<bool> UpdateCargo(string id);
        Task<Cargo> GetCargoById(string id);
        Task<bool> UpdateCargo(string id, string callsign);
        Task<bool> UnloadCargo(string id);
        Task<bool> UpdateCargoLocation(string id, string location);
        Task<List<Cargo>> GetCargos(string location);
    }
}
