using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlomtManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddChartYScalingType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChartYScalingType",
                table: "Parameters",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "ChartYZoom",
                table: "Parameters",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChartYScalingType",
                table: "Parameters");

            migrationBuilder.DropColumn(
                name: "ChartYZoom",
                table: "Parameters");
        }
    }
}
