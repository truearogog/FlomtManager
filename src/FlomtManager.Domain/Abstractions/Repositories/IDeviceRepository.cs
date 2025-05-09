﻿using FlomtManager.Domain.Models;

namespace FlomtManager.Domain.Abstractions.Repositories
{
    public interface IDeviceRepository
    {
        Task<IEnumerable<Device>> GetAll();
        Task<IAsyncEnumerable<Device>> GetAllAsync();
        Task<Device> GetById(int id);
        Task<Device> GetLastCreated();
        Task<Device> GetLastCreatedExcept(int id);
        Task<DeviceDefinition> GetDefinitionByDeviceId(int id);
        Task<int> Create(Device device);
        Task<int> CreateDefinition(DeviceDefinition deviceDefinition);
        Task UpdateDefinitionLastArchiveRead(int deviceId, DateTime? dateTime = null);
        Task Update(Device device);
        Task Delete(int id);
        Task SetRealTimeValues(int deviceId, string values);
        Task<string> GetRealTimeValues(int deviceId);
    }
}
