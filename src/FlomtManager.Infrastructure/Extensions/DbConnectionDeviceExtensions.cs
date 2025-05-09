﻿using System.Data.Common;
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
            SELECT * FROM Devices WHERE Id = @Id LIMIT 1;
            """, new { Id = id });
    }

    public static async Task<Device> GetLastCreatedDevice(this DbConnection connection)
    {
        return await connection.QueryFirstOrDefaultAsync<Device>("""
            SELECT * FROM Devices ORDER BY Created DESC LIMIT 1;
            """);
    }

    public static async Task<Device> GetLastCreatedDeviceExcept(this DbConnection connection, int id)
    {
        return await connection.QueryFirstOrDefaultAsync<Device>("""
            SELECT * FROM Devices WHERE Id <> @Id ORDER BY Created DESC LIMIT 1;
            """, new { Id = id});
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
                DataReadEnabled,
                DataReadIntervalTicks, 

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
                @DataReadEnabled,
                @DataReadIntervalTicks, 
                
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
                DataReadEnabled = @DataReadEnabled,
                DataReadIntervalTicks = @DataReadIntervalTicks, 

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
        await connection.ExecuteAsync($"""
            DELETE FROM Parameters WHERE DeviceId = @Id;
            DELETE FROM DeviceDefinitions WHERE DeviceId = @Id;
            DELETE FROM Devices WHERE Id = @Id;
            DROP TABLE IF EXISTS Data_{id};
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

    public static async Task SetRealTimeValues(this DbConnection connection, int deviceId, string values)
    {
        await connection.QueryFirstOrDefaultAsync<string>("""
            INSERT OR REPLACE INTO DeviceRealTimeValues 
                (DeviceId, RealTimeValues) 
            VALUES
                (@DeviceId, @Values);
            """, new { DeviceId = deviceId, Values = values });
    }

    public static async Task<string> GetRealTimeValues(this DbConnection connection, int deviceId)
    {
        return await connection.QueryFirstOrDefaultAsync<string>("""
            SELECT RealTimeValues 
            FROM DeviceRealTimeValues 
            WHERE DeviceId = @DeviceId;
            """, new { DeviceId = deviceId });
    }
}