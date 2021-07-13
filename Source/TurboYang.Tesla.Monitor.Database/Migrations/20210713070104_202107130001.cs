using Microsoft.EntityFrameworkCore.Migrations;

namespace TurboYang.Tesla.Monitor.Database.Migrations
{
    public partial class _202107130001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TemperatureAverage",
                table: "Driving",
                newName: "OutsideTemperatureAverage");

            migrationBuilder.AddColumn<decimal>(
                name: "InsideTemperatureAverage",
                table: "Driving",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InsideTemperatureAverage",
                table: "Charging",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OutsideTemperatureAverage",
                table: "Charging",
                type: "numeric",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InsideTemperatureAverage",
                table: "Driving");

            migrationBuilder.DropColumn(
                name: "InsideTemperatureAverage",
                table: "Charging");

            migrationBuilder.DropColumn(
                name: "OutsideTemperatureAverage",
                table: "Charging");

            migrationBuilder.RenameColumn(
                name: "OutsideTemperatureAverage",
                table: "Driving",
                newName: "TemperatureAverage");
        }
    }
}
