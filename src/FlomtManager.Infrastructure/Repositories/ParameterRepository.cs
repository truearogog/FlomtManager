using FlomtManager.Domain.Abstractions.Data;
using FlomtManager.Domain.Abstractions.Providers;
using FlomtManager.Domain.Abstractions.Repositories;
using FlomtManager.Domain.Models;
using FlomtManager.Infrastructure.Extensions;

namespace FlomtManager.Infrastructure.Repositories;

internal sealed class ParameterRepository(IDbConnectionFactory connectionFactory, IDateTimeProvider dateTimeProvider) : IParameterRepository
{
    public async Task<int> Create(Parameter parameter)
    {
        var now = dateTimeProvider.Now;
        parameter = parameter with { Created = now, Updated = now };
        await using var connection = connectionFactory.CreateConnection();
        return await connection.CreateParameter(parameter);
    }

    public async Task Create(IEnumerable<Parameter> parameters)
    {
        var now = dateTimeProvider.Now;
        parameters = parameters.Select(parameter => parameter with { Created = now, Updated = now });
        await using var connection = connectionFactory.CreateConnection();
        await connection.CreateParameters(parameters);
    }

    public async Task UpdateIsEnabled(int id, bool isEnabled)
    {
        var now = dateTimeProvider.Now;
        await using var connection = connectionFactory.CreateConnection();
        await connection.UpdateIsEnabled(id, isEnabled, now);
    }

    public async Task UpdateIsAxisVisibleOnChart(int id, bool isAxisVisibleOnChart)
    {
        var now = dateTimeProvider.Now;
        await using var connection = connectionFactory.CreateConnection();
        await connection.UpdateIsAxisVisibleOnChart(id, isAxisVisibleOnChart, now);
    }

    public async Task UpdateIsAutoScaledOnChart(int id, bool isAutoScaledOnChart)
    {
        var now = dateTimeProvider.Now;
        await using var connection = connectionFactory.CreateConnection();
        await connection.UpdateIsAutoScaledOnChart(id, isAutoScaledOnChart, now);
    }

    public async Task UpdateZoomLevelOnChart(int id, double zoomLevelOnChart)
    {
        var now = dateTimeProvider.Now;
        await using var connection = connectionFactory.CreateConnection();
        await connection.UpdateZoomLevelOnChart(id, zoomLevelOnChart, now);
    }

    public async Task UpdateColor(int id, string color)
    {
        var now = dateTimeProvider.Now;
        await using var connection = connectionFactory.CreateConnection();
        await connection.UpdateColor(id, color, now);
    }

    public async Task<IEnumerable<Parameter>> GetAllByDeviceId(int deviceId)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetAllParametersByDeviceId(deviceId);
    }

    public async Task<IEnumerable<Parameter>> GetCurrentParametersByDeviceId(int deviceId, bool all = false)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetCurrentParametersByDeviceId(deviceId, all);
    }

    public async Task<IEnumerable<Parameter>> GetIntegralParametersByDeviceId(int deviceId, bool all = false)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetIntegralParametersByDeviceId(deviceId, all);
    }

    public async Task<IEnumerable<Parameter>> GetHourArchiveParametersByDeviceId(int deviceId, bool all = false)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetHourArchiveParametersByDeviceId(deviceId, all);
    }

    public async Task<IReadOnlyDictionary<Parameter, Parameter>> GetHourArchiveIntegralParametersByDeviceId(int deviceId)
    {
        await using var connection = connectionFactory.CreateConnection();
        return await connection.GetHourArchiveIntegralParametersByDeviceId(deviceId);
    }
}
