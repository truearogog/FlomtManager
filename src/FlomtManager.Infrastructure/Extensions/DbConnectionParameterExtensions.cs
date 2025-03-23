using System.Collections.Frozen;
using System.Data.Common;
using Dapper;
using FlomtManager.Domain.Extensions;
using FlomtManager.Domain.Models;

namespace FlomtManager.Infrastructure.Extensions;

internal static class DbConnectionParameterExtensions
{
    private const string InsertSql = """
        INSERT INTO Parameters (
            Created, 
            Updated, 
            DeviceId,

            Number, 
            Type, 
            Comma, 
            ErrorMask, 
            IntegrationNumber, 

            Name, 
            Unit, 
            Color, 

            IsAxisVisibleOnChart,
            IsAutoScaledOnChart, 
            ZoomLevelOnChart
        ) VALUES (
            @Created, 
            @Updated, 
            @DeviceId,

            @Number, 
            @Type, 
            @Comma, 
            @ErrorMask, 
            @IntegrationNumber, 
            
            @Name, 
            @Unit, 
            @Color, 
            
            @IsAxisVisibleOnChart,
            @IsAutoScaledOnChart, 
            @ZoomLevelOnChart
        );
        """;

    public static async Task<int> CreateParameter(this DbConnection connection, Parameter parameter)
    {
        return await connection.QuerySingleAsync<int>($"""
            ${InsertSql}
            SELECT last_insert_rowid();
            """, parameter);
    }

    public static async Task CreateParameters(this DbConnection connection, IEnumerable<Parameter> parameters)
    {
        await connection.ExecuteAsync(InsertSql, parameters);
    }

    public static async Task UpdateIsAxisVisibleOnChart(this DbConnection connection, int id, bool isAxisVisibleOnChart, DateTime now)
    {
        await connection.ExecuteAsync("""
            UPDATE Parameters 
            SET 
                IsAxisVisibleOnChart = @IsAxisVisibleOnChart,
                Updated = @Now
            WHERE Id = @Id;
            """, new { Id = id, IsAxisVisibleOnChart = isAxisVisibleOnChart, Now = now });
    }

    public static async Task UpdateIsAutoScaledOnChart(this DbConnection connection, int id, bool isAutoScaledOnChart, DateTime now)
    {
        await connection.ExecuteAsync("""
            UPDATE Parameters 
            SET 
                IsAutoScaledOnChart = @IsAutoScaledOnChart,
                Updated = @Now
            WHERE Id = @Id;
            """, new { Id = id, IsAutoScaledOnChart = isAutoScaledOnChart, Now = now });
    }

    public static async Task UpdateZoomLevelOnChart(this DbConnection connection, int id, double zoomLevelOnChart, DateTime now)
    {
        await connection.ExecuteAsync("""
            UPDATE Parameters 
            SET 
                ZoomLevelOnChart = @ZoomLevelOnChart,
                Updated = @Now
            WHERE Id = @Id;
            """, new { Id = id, ZoomLevelOnChart = zoomLevelOnChart, Now = now });
    }

    public static async Task UpdateColor(this DbConnection connection, int id, string color, DateTime now)
    {
        await connection.ExecuteAsync("""
            UPDATE Parameters 
            SET 
                Color = @Color,
                Updated = @Now
            WHERE Id = @Id;
            """, new { Id = id, Color = color, Now = now });
    }

    public static async Task<IEnumerable<Parameter>> GetAllParametersByDeviceId(this DbConnection connection, int deviceId)
    {
        return await connection.QueryAsync<Parameter>("""
            SELECT * FROM Parameters WHERE DeviceId = @DeviceId;
            """, new { DeviceId = deviceId });
    }

    public static async Task<IEnumerable<Parameter>> GetCurrentParametersByDeviceId(this DbConnection connection, int deviceId, bool all = false)
        => await connection.GetParameters("CurrentParameterLineDefinition", deviceId, all);

    public static async Task<IEnumerable<Parameter>> GetIntegralParametersByDeviceId(this DbConnection connection, int deviceId, bool all = false)
        => await connection.GetParameters("IntegralParameterLineDefinition", deviceId, all);

    public static async Task<IEnumerable<Parameter>> GetHourArchiveParametersByDeviceId(this DbConnection connection, int deviceId, bool all = false)
        => await connection.GetParameters("AverageParameterArchiveLineDefinition", deviceId, all);

    public static async Task<IReadOnlyDictionary<Parameter, Parameter>> GetHourArchiveIntegralParametersByDeviceId(this DbConnection connection, int deviceId)
    {
        var allParameters = await connection.GetAllParametersByDeviceId(deviceId);
        var parameters = await connection.GetParameters("AverageParameterArchiveLineDefinition", deviceId, true);
        return parameters
            .Where(x => x.IntegrationNumber != 0)
            .Select(x => new
            {
                Number = x,
                Parameter = allParameters.FirstOrDefault(y => y.Number == x.IntegrationNumber)
            })
            .Where(x => x.Parameter != default)
            .ToFrozenDictionary(x => x.Number, x => x.Parameter);
    }

    private static async Task<IEnumerable<Parameter>> GetParameters(this DbConnection connection, string lineDefinitionSqlColumnName, int deviceId, bool all = false)
    {
        using var multi = await connection.QueryMultipleAsync($"""
            SELECT {lineDefinitionSqlColumnName} FROM DeviceDefinitions WHERE DeviceId = @DeviceId;
            SELECT * FROM Parameters WHERE DeviceId = @DeviceId;
            """, new { DeviceId = deviceId });

        var parameterLineDefinition = await multi.ReadFirstOrDefaultAsync<byte[]>();
        var parameters = await multi.ReadAsync<Parameter>();

        if (parameterLineDefinition is null)
        {
            return [];
        }

        var resultParameters = new List<Parameter>();

        foreach (var parameterByte in parameterLineDefinition)
        {
            if ((parameterByte & 0x80) == 0)
            {
                var parameter = parameters.First(x => x.Number == parameterByte);
                if (all || !parameter.Type.HideType())
                {
                    resultParameters.Add(parameter);
                }
            }
        }

        return resultParameters.AsReadOnly();
    }
}
