using Microsoft.EntityFrameworkCore.Migrations;

namespace TurboYang.Tesla.Monitor.Database.Migrations
{
    public partial class _202107010001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdealBatteryRange",
                table: "StandBySnapshot");

            migrationBuilder.DropColumn(
                name: "EndIdealBatteryRange",
                table: "StandBy");

            migrationBuilder.DropColumn(
                name: "EndRatedBatteryRange",
                table: "StandBy");

            migrationBuilder.DropColumn(
                name: "IdealBatteryRange",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "IdealBatteryRange",
                table: "DrivingSnapshot");

            migrationBuilder.DropColumn(
                name: "EndIdealBatteryRange",
                table: "Driving");

            migrationBuilder.DropColumn(
                name: "EndRatedBatteryRange",
                table: "Driving");

            migrationBuilder.DropColumn(
                name: "IdealBatteryRange",
                table: "ChargingSnapshot");

            migrationBuilder.DropColumn(
                name: "EndIdealBatteryRange",
                table: "Charging");

            migrationBuilder.DropColumn(
                name: "EndRatedBatteryRange",
                table: "Charging");

            migrationBuilder.RenameColumn(
                name: "RatedBatteryRange",
                table: "StandBySnapshot",
                newName: "BatteryRange");

            migrationBuilder.RenameColumn(
                name: "StartRatedBatteryRange",
                table: "StandBy",
                newName: "StartBatteryRange");

            migrationBuilder.RenameColumn(
                name: "StartIdealBatteryRange",
                table: "StandBy",
                newName: "EndBatteryRange");

            migrationBuilder.RenameColumn(
                name: "RatedBatteryRange",
                table: "Snapshot",
                newName: "BatteryRange");

            migrationBuilder.RenameColumn(
                name: "RatedBatteryRange",
                table: "DrivingSnapshot",
                newName: "BatteryRange");

            migrationBuilder.RenameColumn(
                name: "StartRatedBatteryRange",
                table: "Driving",
                newName: "StartBatteryRange");

            migrationBuilder.RenameColumn(
                name: "StartIdealBatteryRange",
                table: "Driving",
                newName: "EndBatteryRange");

            migrationBuilder.RenameColumn(
                name: "RatedBatteryRange",
                table: "ChargingSnapshot",
                newName: "BatteryRange");

            migrationBuilder.RenameColumn(
                name: "StartRatedBatteryRange",
                table: "Charging",
                newName: "StartBatteryRange");

            migrationBuilder.RenameColumn(
                name: "StartIdealBatteryRange",
                table: "Charging",
                newName: "EndBatteryRange");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BatteryRange",
                table: "StandBySnapshot",
                newName: "RatedBatteryRange");

            migrationBuilder.RenameColumn(
                name: "StartBatteryRange",
                table: "StandBy",
                newName: "StartRatedBatteryRange");

            migrationBuilder.RenameColumn(
                name: "EndBatteryRange",
                table: "StandBy",
                newName: "StartIdealBatteryRange");

            migrationBuilder.RenameColumn(
                name: "BatteryRange",
                table: "Snapshot",
                newName: "RatedBatteryRange");

            migrationBuilder.RenameColumn(
                name: "BatteryRange",
                table: "DrivingSnapshot",
                newName: "RatedBatteryRange");

            migrationBuilder.RenameColumn(
                name: "StartBatteryRange",
                table: "Driving",
                newName: "StartRatedBatteryRange");

            migrationBuilder.RenameColumn(
                name: "EndBatteryRange",
                table: "Driving",
                newName: "StartIdealBatteryRange");

            migrationBuilder.RenameColumn(
                name: "BatteryRange",
                table: "ChargingSnapshot",
                newName: "RatedBatteryRange");

            migrationBuilder.RenameColumn(
                name: "StartBatteryRange",
                table: "Charging",
                newName: "StartRatedBatteryRange");

            migrationBuilder.RenameColumn(
                name: "EndBatteryRange",
                table: "Charging",
                newName: "StartIdealBatteryRange");

            migrationBuilder.AddColumn<decimal>(
                name: "IdealBatteryRange",
                table: "StandBySnapshot",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EndIdealBatteryRange",
                table: "StandBy",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EndRatedBatteryRange",
                table: "StandBy",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IdealBatteryRange",
                table: "Snapshot",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IdealBatteryRange",
                table: "DrivingSnapshot",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EndIdealBatteryRange",
                table: "Driving",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EndRatedBatteryRange",
                table: "Driving",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "IdealBatteryRange",
                table: "ChargingSnapshot",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EndIdealBatteryRange",
                table: "Charging",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EndRatedBatteryRange",
                table: "Charging",
                type: "numeric",
                nullable: true);
        }
    }
}
