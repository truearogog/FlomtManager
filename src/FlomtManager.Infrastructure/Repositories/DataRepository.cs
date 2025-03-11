using System.Collections.Frozen;
using System.Data;
using System.Data.Common;
using System.Text;
using Dapper;
using FlomtManager.Core.Data;
using FlomtManager.Core.Extensions;
using FlomtManager.Core.Models.Collections;
using FlomtManager.Core.Repositories;
using FlomtManager.Framework.Extensions;
using FlomtManager.Infrastructure.Extensions;

namespace FlomtManager.Infrastructure.Repositories;

internal sealed class DataRepository(IDbConnectionFactory connectionFactory) : IDataRepository
{
    public async Task InitDataTables(int deviceId)
    {
        await InitHourArchive(deviceId);
    }

    public async Task<bool> HasHourData(int deviceId)
    {
        await using var connection = connectionFactory.CreateConnection();

        return await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@TableName;
            """, new { TableName = "HourArchive_" + deviceId }) != 0;
    }

    // take from end
    public async Task AddHourData(int deviceId, IReadOnlyDictionary<byte, IDataCollection> data, int size, int actualSize)
    {
        await using var connection = connectionFactory.CreateConnection();

        var parameters = await connection.GetHourArchiveParametersByDeviceId(deviceId, true);

        var dateTimeCollection = (DataCollection<DateTime>)data[0];

        var columnNames = string.Join(", ", data.Keys
            .Except([(byte)0])
            .Select(parameterNumber => $"p{parameterNumber}")
            .Prepend("DateTime"));

        var sb = new StringBuilder();
        sb.Append("INSERT INTO HourArchive_");
        sb.Append(deviceId);
        sb.Append(" (");
        sb.Append(columnNames);
        sb.AppendLine(") VALUES");

        var dynamicParameters = new DynamicParameters();
        var dynamicParameterIndex = 0;

        for (var i = size - actualSize; i < size; i++)
        {
            sb.Append('(');
            var dynamicParameter = "p" + dynamicParameterIndex++;
            dynamicParameters.Add(dynamicParameter, dateTimeCollection.Values[i]);
            sb.Append('@');
            sb.Append(dynamicParameter);
            sb.Append(',');
            foreach (var parameter in parameters.OrderBy(x => x.Number))
            {
                sb.Append('\'');
                if (data[parameter.Number] is DataCollection<float> floatDataCollection)
                {
                    var value = floatDataCollection.Values[i];
                    sb.Append(value.ToString().Replace(',', '.'));
                }
                else if (data[parameter.Number] is DataCollection<uint> uintDataCollection)
                {
                    var value = uintDataCollection.Values[i];
                    sb.Append(value);
                }
                else if (data[parameter.Number] is DataCollection<ushort> ushortDataCollection)
                {
                    var value = ushortDataCollection.Values[i];
                    sb.Append(value);
                }
                else if (data[parameter.Number] is DataCollection<TimeSpan> timeSpanDataCollection)
                {
                    var value = timeSpanDataCollection.Values[i];
                    sb.Append(value);
                }
                else if (data[parameter.Number] is DataCollection<DateTime> dateTimeDataCollection)
                {
                    var value = dateTimeDataCollection.Values[i];
                    sb.Append(value);
                }
                sb.Append('\'');
                sb.Append(',');
            }

            sb.RemoveLast(1);
            sb.AppendLine("),");
        }
        sb.RemoveLast(3);
        sb.Append(';');

        var sql = sb.ToString();

        await connection.ExecuteAsync(sql, dynamicParameters);
    }

    public async Task<IReadOnlyDictionary<byte, IDataCollection>> GetHourData(int deviceId)
    {
        await using var connection = connectionFactory.CreateConnection();

        var parameters = (await connection.GetHourArchiveParametersByDeviceId(deviceId, true)).ToFrozenDictionary(x => x.Number);

        var dataPointCount = await connection.ExecuteScalarAsync<int>($"""
            SELECT COUNT(*) FROM HourArchive_{deviceId};
            """);

        var dataCollectionFactories = new Dictionary<Type, Func<IDataCollection>>
        {
            [typeof(float)] = () => new DataCollection<float>(dataPointCount),
            [typeof(uint)] = () => new DataCollection<uint>(dataPointCount),
            [typeof(ushort)] = () => new DataCollection<ushort>(dataPointCount),
            [typeof(DateTime)] = () => new DataCollection<DateTime>(dataPointCount),
            [typeof(TimeSpan)] = () => new DataCollection<TimeSpan>(dataPointCount),
        };

        var parameterNumbers = new byte[parameters.Count + 1];
        parameterNumbers[0] = 0;
        parameters.Keys.CopyTo(parameterNumbers, 1);

        var data = parameterNumbers
            .Select(x => new
            {
                Number = x,
                ClrType = x switch
                {
                    0 => typeof(DateTime),
                    _ => parameters[x].Type.GetClrType(),
                }
            })
            .Select(x => new
            {
                x.Number,
                DataCollection = dataCollectionFactories.TryGetValue(x.ClrType, out var factory) ? factory() : null,
            })
            .Where(x => x.DataCollection is not null)
            .ToFrozenDictionary(x => x.Number, x => x.DataCollection);

        foreach (var parameterNumber in parameterNumbers)
        {
            var column = parameterNumber switch
            {
                0 => "DateTime",
                _ => "p" + parameterNumber,
            };

            if (data[parameterNumber] is DataCollection<float> floatDataCollection)
            {
                var values = (await GetArchiveData<float>(connection, column, deviceId)).ToArray();
                values.CopyTo(floatDataCollection.Values, 0);
            }
            else if (data[parameterNumber] is DataCollection<uint> uintDataCollection)
            {
                var values = (await GetArchiveData<uint>(connection, column, deviceId)).ToArray();
                values.CopyTo(uintDataCollection.Values, 0);
            }
            else if (data[parameterNumber] is DataCollection<ushort> ushortDataCollection)
            {
                var values = (await GetArchiveData<ushort>(connection, column, deviceId)).ToArray();
                values.CopyTo(ushortDataCollection.Values, 0);
            }
            else if (data[parameterNumber] is DataCollection<TimeSpan> timeSpanDataCollection)
            {
                var values = (await GetArchiveData<TimeSpan>(connection, column, deviceId)).ToArray();
                values.CopyTo(timeSpanDataCollection.Values, 0);
            }
            else if (data[parameterNumber] is DataCollection<DateTime> dateTimeDataCollection)
            {
                var values = (await GetArchiveData<DateTime>(connection, column, deviceId)).ToArray();
                values.CopyTo(dateTimeDataCollection.Values, 0);
            }
        }

        return data;
    }

    private static async Task<IEnumerable<T>> GetArchiveData<T>(DbConnection connection, string column, int deviceId) => 
        await connection.QueryAsync<T>($"""
            SELECT {column} FROM HourArchive_{deviceId};
            """);

    private async Task<bool> InitHourArchive(int deviceId)
    {
        await using var connection = connectionFactory.CreateConnection();

        var parameters = await connection.GetHourArchiveParametersByDeviceId(deviceId, true);

        var sb = new StringBuilder();
        foreach (var parameter in parameters)
        {
            sb.Append('p');
            sb.Append(parameter.Number);
            sb.Append(' ');
            sb.Append(parameter.Type.GetSqlType());
            sb.Append(", ");
        }
        sb.RemoveLast(2);
        var tables = sb.ToString();

        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS HourArchive_{deviceId} (
                DateTime {SqlTypes.DateTime} PRIMARY KEY,
                {tables}
            );
            """);

        return true;
    }
}
