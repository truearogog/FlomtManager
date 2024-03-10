using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomtManager.Data.EF.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    ConnectionType = table.Column<int>(type: "INTEGER", nullable: false),
                    SlaveId = table.Column<byte>(type: "INTEGER", nullable: false),
                    PortName = table.Column<string>(type: "TEXT", nullable: true),
                    BaudRate = table.Column<int>(type: "INTEGER", nullable: false),
                    Parity = table.Column<int>(type: "INTEGER", nullable: false),
                    DataBits = table.Column<int>(type: "INTEGER", nullable: false),
                    StopBits = table.Column<int>(type: "INTEGER", nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", nullable: true),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    DeviceDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "datetime('now', 'localtime')"),
                    Updated = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Data = table.Column<byte[]>(type: "BLOB", nullable: false),
                    DeviceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "datetime('now', 'localtime')"),
                    Updated = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataGroups_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    ParameterDefinitionStart = table.Column<ushort>(type: "INTEGER", nullable: false),
                    ParameterDefinitionNumber = table.Column<ushort>(type: "INTEGER", nullable: false),
                    DescriptionStart = table.Column<ushort>(type: "INTEGER", nullable: false),
                    ProgramVersionStart = table.Column<ushort>(type: "INTEGER", nullable: false),
                    CurrentParameterLineDefinitionStart = table.Column<ushort>(type: "INTEGER", nullable: false),
                    CurrentParameterLineDefinition = table.Column<byte[]>(type: "BLOB", nullable: true),
                    CurrentParameterLineLength = table.Column<byte>(type: "INTEGER", nullable: false),
                    CurrentParameterLineNumber = table.Column<byte>(type: "INTEGER", nullable: false),
                    CurrentParameterLineStart = table.Column<ushort>(type: "INTEGER", nullable: false),
                    IntegralParameterLineDefinitionStart = table.Column<ushort>(type: "INTEGER", nullable: false),
                    IntegralParameterLineDefinition = table.Column<byte[]>(type: "BLOB", nullable: true),
                    IntegralParameterLineLength = table.Column<byte>(type: "INTEGER", nullable: false),
                    IntegralParameterLineNumber = table.Column<byte>(type: "INTEGER", nullable: false),
                    IntegralParameterLineStart = table.Column<ushort>(type: "INTEGER", nullable: false),
                    AverageParameterArchiveLineDefinitionStart = table.Column<ushort>(type: "INTEGER", nullable: false),
                    AverageParameterArchiveLineDefinition = table.Column<byte[]>(type: "BLOB", nullable: true),
                    AverageParameterArchiveLineLength = table.Column<byte>(type: "INTEGER", nullable: false),
                    AverageParameterArchiveLineNumber = table.Column<byte>(type: "INTEGER", nullable: false),
                    AveragePerHourBlockStart = table.Column<ushort>(type: "INTEGER", nullable: false),
                    AveragePerHourBlockLineCount = table.Column<ushort>(type: "INTEGER", nullable: false),
                    CRC = table.Column<ushort>(type: "INTEGER", nullable: false),
                    LastArchiveRead = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "datetime('now', 'localtime')"),
                    Updated = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceDefinitions_Devices_Id",
                        column: x => x.Id,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Parameters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<byte>(type: "INTEGER", nullable: false),
                    ParameterType = table.Column<string>(type: "TEXT", nullable: false),
                    Comma = table.Column<byte>(type: "INTEGER", nullable: false),
                    ErrorMask = table.Column<ushort>(type: "INTEGER", nullable: false),
                    IntegrationNumber = table.Column<byte>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 4, nullable: false),
                    Unit = table.Column<string>(type: "TEXT", maxLength: 6, nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    DeviceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "datetime('now', 'localtime')"),
                    Updated = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parameters_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataGroups_DateTime_DeviceId",
                table: "DataGroups",
                columns: new[] { "DateTime", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataGroups_DeviceId",
                table: "DataGroups",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Parameters_DeviceId",
                table: "Parameters",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataGroups");

            migrationBuilder.DropTable(
                name: "DeviceDefinitions");

            migrationBuilder.DropTable(
                name: "Parameters");

            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
