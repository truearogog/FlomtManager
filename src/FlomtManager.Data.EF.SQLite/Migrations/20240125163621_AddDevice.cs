using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomtManager.Data.EF.SQLite.Migrations
{
    /// <inheritdoc />
    public partial class AddDevice : Migration
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
                    SerialCode = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    MeterNr = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValue: new DateTime(2024, 1, 25, 16, 36, 21, 223, DateTimeKind.Utc).AddTicks(2874)),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValue: new DateTime(2024, 1, 25, 16, 36, 21, 223, DateTimeKind.Utc).AddTicks(3198))
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Devices");
        }
    }
}
