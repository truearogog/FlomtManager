using System.Data;
using System.Data.Common;
using Dapper;
using FlomtManager.Core;
using FlomtManager.Domain.Enums;

namespace FlomtManager.Infrastructure.Data.Migrations;

internal sealed class Initial : Migration
{
    protected override async Task Up(DbConnection connection, IDbTransaction transaction)
    {
        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS Devices (
                Id {SqlTypes.Int} PRIMARY KEY AUTOINCREMENT,
                Created {SqlTypes.DateTime} NOT NULL,
                Updated {SqlTypes.DateTime} NOT NULL,

                Name NVARCHAR(30), 
                Address NVARCHAR(50),

                ConnectionType {SqlTypes.Int},
                SlaveId {SqlTypes.Int},
                DataReadIntervalTicks {SqlTypes.Int},

                IpAddress NVARCHAR(30),
                PortName CHAR(10),

                BaudRate {SqlTypes.Int},
                Parity {SqlTypes.Int},
                DataBits {SqlTypes.Int},
                StopBits {SqlTypes.Int},
                Port {SqlTypes.Int}
            );
            """, transaction);

        await connection.ExecuteAsync($"""
            INSERT INTO Devices (Created, Updated, Name, Address, ConnectionType, SlaveId, DataReadIntervalTicks, PortName, BaudRate, Parity, DataBits, StopBits, IpAddress, Port)
            VALUES (@Now, @Now, @Name, @Address, @ConnectionType, @SlaveId, @DataReadIntervalTicks, @PortName, @BaudRate, @Parity, @DataBits, @StopBits, @IpAddress, @Port);
            """, new
        {
            Now = DateTime.UtcNow,

            Name = "SF-38",
            Address = "Kāvu 8",

            ConnectionType = ConnectionType.Network,
            SlaveId = 1,
            DataReadIntervalTicks = TimeSpan.FromSeconds(5).Ticks,

            PortName = "COM1",
            BaudRate = 9600,
            Parity = 0,
            DataBits = 8,
            StopBits = 1,

            IpAddress = "185.147.58.54",
            Port = 5000
        }, transaction);

        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS "DeviceDefinitions" (
                Id {SqlTypes.Int} PRIMARY KEY AUTOINCREMENT,
            	Created {SqlTypes.DateTime},
            	Updated {SqlTypes.DateTime},
                DeviceId {SqlTypes.Int},

            	ParameterDefinitionStart {SqlTypes.Int} NOT NULL,
            	ParameterDefinitionNumber {SqlTypes.Int} NOT NULL,

            	DescriptionStart {SqlTypes.Int} NOT NULL,
            	ProgramVersionStart {SqlTypes.Int} NOT NULL,

            	CurrentParameterLineDefinitionStart {SqlTypes.Int} NOT NULL,
            	CurrentParameterLineDefinition BLOB NULL,
            	CurrentParameterLineLength {SqlTypes.Int} NOT NULL,
            	CurrentParameterLineNumber {SqlTypes.Int} NOT NULL,
            	CurrentParameterLineStart {SqlTypes.Int} NOT NULL,

            	IntegralParameterLineDefinitionStart {SqlTypes.Int} NOT NULL,
            	IntegralParameterLineDefinition BLOB NULL,
            	IntegralParameterLineLength {SqlTypes.Int} NOT NULL,
            	IntegralParameterLineNumber {SqlTypes.Int} NOT NULL,
            	IntegralParameterLineStart {SqlTypes.Int} NOT NULL,

            	AverageParameterArchiveLineDefinitionStart {SqlTypes.Int} NOT NULL,
            	AverageParameterArchiveLineDefinition BLOB NULL,
            	AverageParameterArchiveLineLength {SqlTypes.Int} NOT NULL,
            	AverageParameterArchiveLineNumber {SqlTypes.Int} NOT NULL,

            	AveragePerHourBlockStart {SqlTypes.Int} NOT NULL,
            	AveragePerHourBlockLineCount {SqlTypes.Int} NOT NULL,

            	CRC {SqlTypes.Int} NOT NULL,
            	LastArchiveRead {SqlTypes.DateTime} NULL,

                FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
            )
            ;
            """);

        await connection.ExecuteAsync($"""
            CREATE TABLE IF NOT EXISTS Parameters (
                Id {SqlTypes.Int} PRIMARY KEY AUTOINCREMENT,
            	Created {SqlTypes.DateTime},
            	Updated {SqlTypes.DateTime},
                DeviceId {SqlTypes.Int},

                Number {SqlTypes.Int},
                Type {SqlTypes.Int},
                Comma {SqlTypes.Int},
                ErrorMask {SqlTypes.Int},
                IntegrationNumber {SqlTypes.Int},

                Name CHAR(4),
                Unit CHAR(6),
                Color TEXT,

                YAxisIsVisible {SqlTypes.Int},
                YAxisScalingType {SqlTypes.Int},
                YAxisZoom {SqlTypes.Real},

                FOREIGN KEY (DeviceId) REFERENCES Devices(Id) ON DELETE CASCADE
            );
            """);
    }

    protected override async Task Down(DbConnection connection, IDbTransaction transaction)
    {
        await connection.ExecuteAsync("DROP TABLE IF EXISTS Parameters;", transaction);

        await connection.ExecuteAsync("DROP TABLE IF EXISTS DeviceDefinitions;", transaction);

        if ((await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Devices';", transaction)) != 0)
        {
            var deviceIds = await connection.QueryAsync<int>("SELECT Id FROM Devices;", transaction);
            foreach (var deviceId in deviceIds)
            {
                await connection.ExecuteAsync($"DROP TABLE IF EXISTS HourArchive_{deviceId};", transaction);
            }
        }

        await connection.ExecuteAsync("DROP TABLE IF EXISTS Devices;", transaction);
    }
}
