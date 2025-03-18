using System.Data.Common;
using Dapper;
using FlomtManager.Domain.Models;

namespace FlomtManager.Infrastructure.Extensions;

internal static class DbConnectionDeviceExtensions
{
    public static async Task<IEnumerable<Device>> GetAllDevices(this DbConnection connection)
    {
        return await connection.QueryAsync<Device>("""
            SELECT * FROM Devices;
            """);
    }

    public static IAsyncEnumerable<Device> GetAllDevicesAsync(this DbConnection connection)
    {
        return connection.QueryUnbufferedAsync<Device>("""
            SELECT * FROM Devices;
            """);
    }

    public static async Task<Device> GetDeviceById(this DbConnection connection, int id)
    {
        return await connection.QueryFirstOrDefaultAsync<Device>("""
            SELECT * FROM Devices WHERE Id = @Id;
            """, new { Id = id });
    }

    public static async Task<int> CreateDevice(this DbConnection connection, Device device)
    {
        return await connection.QuerySingleAsync<int>("""
            INSERT INTO Devices (
                Created, 
                Updated, 

                Name, 
                Address, 

                ConnectionType, 
                SlaveId, 
                DataReadIntervalHours, 
                DataReadIntervalMinutes, 
                DataReadIntervalSeconds, 

                PortName, 
                BaudRate, 
                Parity, 
                DataBits,
                StopBits, 

                IpAddress, 
                Port
            ) VALUES (
                @Created, 
                @Updated, 

                @Name, 
                @Address, 

                @ConnectionType, 
                @SlaveId, 
                @DataReadIntervalHours, 
                @DataReadIntervalMinutes, 
                @DataReadIntervalSeconds, 
                
                @PortName, 
                @BaudRate, 
                @Parity, 
                @DataBits, 
                @StopBits, 
                
                @IpAddress, 
                @Port
            );
            SELECT last_insert_rowid();
            """, device);
    }

    public static async Task UpdateDevice(this DbConnection connection, Device device)
    {
        await connection.ExecuteAsync("""
            UPDATE Devices
            SET 
                Updated = @Updated,

                Name = @Name,
                Address = @Address,

                ConnectionType = @ConnectionType,
                SlaveId = @SlaveId,
                DataReadIntervalHours = @DataReadIntervalHours, 
                DataReadIntervalMinutes = @DataReadIntervalMinutes, 
                DataReadIntervalSeconds = @DataReadIntervalSeconds, 

                PortName = @PortName,
                BaudRate = @BaudRate,
                Parity = @Parity,
                DataBits = @DataBits,
                StopBits = @StopBits,

                IpAddress = @IpAddress,
                Port = @Port
            WHERE Id = @Id;
            """, device);
    }

    public static async Task DeleteDevice(this DbConnection connection, int id)
    {
        await connection.ExecuteAsync("""
            DELETE FROM Parameters WHERE DeviceId = @Id;
            DELETE FROM DeviceDefinitions WHERE DeviceId = @Id;
            DELETE FROM Devices WHERE Id = @Id;
            DELETE TABLE IF EXISTS Data_@Id;
            """, new { Id = id });
    }

    public static async Task<DeviceDefinition> GetDefinitionByDeviceId(this DbConnection connection, int id)
    {
        return await connection.QueryFirstOrDefaultAsync<DeviceDefinition>("""
            SELECT * FROM DeviceDefinitions WHERE DeviceId = @Id;
            """, new { Id = id });
    }

    public static async Task<int> CreateDefinition(this DbConnection connection, DeviceDefinition deviceDefinition)
    {
        return await connection.QuerySingleAsync<int>("""
            INSERT INTO DeviceDefinitions (
                Created, 
                Updated, 
                DeviceId,

                ParameterDefinitionStart, 
                ParameterDefinitionNumber, 
                DescriptionStart, 
                ProgramVersionStart, 

                CurrentParameterLineDefinitionStart, 
                CurrentParameterLineDefinition, 
                CurrentParameterLineLength, 
                CurrentParameterLineNumber, 
                CurrentParameterLineStart,

                IntegralParameterLineDefinitionStart, 
                IntegralParameterLineDefinition, 
                IntegralParameterLineLength, 
                IntegralParameterLineNumber, 
                IntegralParameterLineStart, 

                AverageParameterArchiveLineDefinitionStart, 
                AverageParameterArchiveLineDefinition, 
                AverageParameterArchiveLineLength, 
                AverageParameterArchiveLineNumber, 

                AveragePerHourBlockStart, 
                AveragePerHourBlockLineCount, 

                CRC, 
                LastArchiveRead
            ) VALUES (
                @Created, 
                @Updated, 
                @DeviceId,

                @ParameterDefinitionStart, 
                @ParameterDefinitionNumber, 
                @DescriptionStart, 
                @ProgramVersionStart, 

                @CurrentParameterLineDefinitionStart, 
                @CurrentParameterLineDefinition, 
                @CurrentParameterLineLength, 
                @CurrentParameterLineNumber, 
                @CurrentParameterLineStart,

                @IntegralParameterLineDefinitionStart, 
                @IntegralParameterLineDefinition, 
                @IntegralParameterLineLength, 
                @IntegralParameterLineNumber, 
                @IntegralParameterLineStart, 

                @AverageParameterArchiveLineDefinitionStart, 
                @AverageParameterArchiveLineDefinition, 
                @AverageParameterArchiveLineLength, 
                @AverageParameterArchiveLineNumber, 

                @AveragePerHourBlockStart, 
                @AveragePerHourBlockLineCount, 

                @CRC, 
                @LastArchiveRead
            );
            SELECT last_insert_rowid();
            """, deviceDefinition);
    }

    public static async Task UpdateDefinitionLastArchiveRead(this DbConnection connection, int deviceId, DateTime time)
    {
        await connection.ExecuteAsync("""
            UPDATE DeviceDefinitions
                SET 
                    Updated = @Time,
                    LastArchiveRead = @Time
            WHERE DeviceId = @DeviceId
            """, new { DeviceId = deviceId, Time = time });
    }
}
