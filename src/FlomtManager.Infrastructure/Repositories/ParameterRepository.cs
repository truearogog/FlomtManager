using FlomtManager.Core.Data;
using FlomtManager.Core.Models;
using FlomtManager.Core.Providers;
using FlomtManager.Core.Repositories;
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
