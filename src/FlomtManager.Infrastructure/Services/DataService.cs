using System.Collections.Frozen;
using FlomtManager.Core.Attributes;
using FlomtManager.Core.Enums;
using FlomtManager.Core.Models;
using FlomtManager.Core.Repositories;
using FlomtManager.Core.Services;
using FlomtManager.Framework.Extensions;
using Microsoft.EntityFrameworkCore;

namespace FlomtManager.Infrastructure.Services;

internal sealed class DataService(
        IDeviceDefinitionRepository deviceDefinitionRepository,
        IDataGroupRepository dataGroupRepository,
        IParameterRepository parameterRepository,
        IModbusService modbusService) : IDataService
{
    private readonly IReadOnlyDictionary<ParameterType, byte> _parameterTypeSizes =
            Enum.GetValues<ParameterType>().ToFrozenDictionary(x => x, x => x.GetAttribute<SizeAttribute>()?.Size ?? throw new Exception("Wrong parameter size."));

    public async Task<DataGroupValues[]> GetDataGroupValues(int deviceId, CancellationToken cancellationToken = default)
    {
        var definition = await deviceDefinitionRepository.GetAll().Where(x => x.Id == deviceId).FirstOrDefaultAsync(cancellationToken);
        if (definition == null)
        {
            return [];
        }
        var parameters = (await parameterRepository.GetAll().Where(x => x.DeviceId == deviceId).ToListAsync()).ToFrozenDictionary(x => x.Number, x => x);
        var dataGroups = await dataGroupRepository.GetAll().Where(x => x.DeviceId == deviceId).OrderBy(x => x.DateTime).ToListAsync(cancellationToken);
        var averageParameterDefinition = definition.AverageParameterArchiveLineDefinition!;
        var realParameters = averageParameterDefinition.Where(parameters.ContainsKey).Select(x => parameters[x]).ToArray();
        var parameterCount = realParameters.Length;

        return dataGroups.AsParallel()
            .Select(x =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = 0;
                var currentValue = 0;
                var values = new object[parameterCount];
                foreach (var parameterByte in averageParameterDefinition)
                {
                    if ((parameterByte & 0x80) == 0)
                    {
                        var parameter = parameters[parameterByte];
                        var size = _parameterTypeSizes[parameter.ParameterType];
                        var value = modbusService.ParseBytesToValue(x.Data.AsSpan(current, size), parameter.ParameterType, parameter.Comma);
                        values[currentValue++] = value;
                        current += size;
                    }
                    else
                    {
                        current += parameterByte & 0xF;
                    }
                }
                return new DataGroupValues
                {
                    DateTime = x.DateTime,
                    Parameters = realParameters,
                    Values = values,
                };
            })
            .ToArray();
    }
}
