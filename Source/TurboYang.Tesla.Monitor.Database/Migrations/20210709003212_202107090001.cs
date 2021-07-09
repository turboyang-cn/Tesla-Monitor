using Microsoft.EntityFrameworkCore.Migrations;

namespace TurboYang.Tesla.Monitor.Database.Migrations
{
    public partial class _202107090001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TemperatureAverage",
                table: "Driving",
                type: "numeric",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemperatureAverage",
                table: "Driving");
        }
    }
}
