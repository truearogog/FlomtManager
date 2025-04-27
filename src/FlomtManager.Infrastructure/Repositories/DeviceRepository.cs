using FlomtManager.Domain.Abstractions.Data;
using FlomtManager.Domain.Abstractions.Providers;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Models;
using FlomtManager.Infrastructure.Extensions;

namespace FlomtManager.Infrastructure.Repositories;

public sealed class DeviceRepository(IDbConnectionFactory connectionFactory, IDateTimeProvider dateTimeProvider) : IDeviceRepository
{
    public async Task<IEnumerable<Device>> GetAll()
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetAllDevices();
    }

    public async Task<IAsyncEnumerable<Device>> GetAllAsync()
    {
        await using var connection = connectionFactory.CreateConnection();
        return connection.GetAllDevicesAsync();
    }

    public async Task<Device> GetById(int id)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetDeviceById(id);
    }

    public async Task<Device> GetLastCreated()
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetLastCreatedDevice();
    }

    public async Task<Device> GetLastCreatedExcept(int id)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetLastCreatedDeviceExcept(id);
    }

    public async Task<int> Create(Device device)
    {
        var now = dateTimeProvider.Now;
        device = device with { Created = now, Updated = now };
        await using var connection = connectionFactory.CreateConnection();
        return await connection.CreateDevice(device);
    }

    public async Task Update(Device device)
    {
        device = device with { Updated = dateTimeProvider.Now };
        await using var connection = connectionFactory.CreateConnection();
        await connection.UpdateDevice(device);
    }

    public async Task Delete(int id)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.DeleteDevice(id);
    }

    public async Task<DeviceDefinition> GetDefinitionByDeviceId(int id)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetDefinitionByDeviceId(id);
    }

    public async Task<int> CreateDefinition(DeviceDefinition deviceDefinition)
    {
        var now = dateTimeProvider.Now;
        deviceDefinition = deviceDefinition with { Created = now, Updated = now };
        await using var connection = connectionFactory.CreateConnection();
        return await connection.CreateDefinition(deviceDefinition);
    }

    public async Task UpdateDefinitionLastArchiveRead(int deviceId, DateTime? dateTime = null)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.UpdateDefinitionLastArchiveRead(deviceId, dateTime ?? dateTimeProvider.Now);
    }

    public async Task SetRealTimeValues(int deviceId, string values)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.SetRealTimeValues(deviceId, values);
    }

    public async Task<string> GetRealTimeValues(int deviceId)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetRealTimeValues(deviceId);
    }
}
