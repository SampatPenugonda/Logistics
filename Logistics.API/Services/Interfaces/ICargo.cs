﻿using Logistics.Models;

namespace Logistics.API.Services.Interfaces
{
    public interface ICargo
    {
        Task<Cargo> AddCargo(string location, string destination);
        Task<bool> UpdateCargo(string id);
        Task<Cargo> GetCargoById(string id);
        Task<Cargo> UpdateCargo(string id, string callsign);
        Task<Cargo> UnloadCargo(string id);
        Task<Cargo> UpdateCargoLocation(string id, string location);
        Task<List<Cargo>> GetCargos(string location);
        string GetLastError();
    }
}
