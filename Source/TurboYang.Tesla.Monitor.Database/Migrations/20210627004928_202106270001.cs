using Microsoft.EntityFrameworkCore.Migrations;

namespace TurboYang.Tesla.Monitor.Database.Migrations
{
    public partial class _202106270001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Token_CreateTimestamp",
                table: "Token");

            migrationBuilder.DropIndex(
                name: "IX_State_CreateTimestamp",
                table: "State");

            migrationBuilder.DropIndex(
                name: "IX_StandBySnapshot_CreateTimestamp",
                table: "StandBySnapshot");

            migrationBuilder.DropIndex(
                name: "IX_StandBy_CreateTimestamp",
                table: "StandBy");

            migrationBuilder.DropIndex(
                name: "IX_Snapshot_CreateTimestamp",
                table: "Snapshot");

            migrationBuilder.DropIndex(
                name: "IX_DrivingSnapshot_CreateTimestamp",
                table: "DrivingSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_Driving_CreateTimestamp",
                table: "Driving");

            migrationBuilder.DropIndex(
                name: "IX_ChargingSnapshot_CreateTimestamp",
                table: "ChargingSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_Charging_CreateTimestamp",
                table: "Charging");

            migrationBuilder.DropIndex(
                name: "IX_CarSetting_CreateTimestamp",
                table: "CarSetting");

            migrationBuilder.DropIndex(
                name: "IX_Car_CreateTimestamp",
                table: "Car");

            migrationBuilder.DropIndex(
                name: "IX_Address_CreateTimestamp",
                table: "Address");

            migrationBuilder.AlterColumn<decimal>(
                name: "Radius",
                table: "Address",
                type: "numeric",
                nullable: false,
                defaultValue: 0.03m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 30m);

            migrationBuilder.CreateIndex(
                name: "IX_StandBySnapshot_Timestamp",
                table: "StandBySnapshot",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_Timestamp",
                table: "Snapshot",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DrivingSnapshot_Timestamp",
                table: "DrivingSnapshot",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSnapshot_Timestamp",
                table: "ChargingSnapshot",
                column: "Timestamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StandBySnapshot_Timestamp",
                table: "StandBySnapshot");

            migrationBuilder.DropIndex(
                name: "IX_Snapshot_Timestamp",
                table: "Snapshot");

            migrationBuilder.DropIndex(
                name: "IX_DrivingSnapshot_Timestamp",
                table: "DrivingSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_ChargingSnapshot_Timestamp",
                table: "ChargingSnapshot");

            migrationBuilder.AlterColumn<decimal>(
                name: "Radius",
                table: "Address",
                type: "numeric",
                nullable: false,
                defaultValue: 30m,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldDefaultValue: 0.03m);

            migrationBuilder.CreateIndex(
                name: "IX_Token_CreateTimestamp",
                table: "Token",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_State_CreateTimestamp",
                table: "State",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_StandBySnapshot_CreateTimestamp",
                table: "StandBySnapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_StandBy_CreateTimestamp",
                table: "StandBy",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_CreateTimestamp",
                table: "Snapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DrivingSnapshot_CreateTimestamp",
                table: "DrivingSnapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Driving_CreateTimestamp",
                table: "Driving",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSnapshot_CreateTimestamp",
                table: "ChargingSnapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Charging_CreateTimestamp",
                table: "Charging",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CarSetting_CreateTimestamp",
                table: "CarSetting",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Car_CreateTimestamp",
                table: "Car",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Address_CreateTimestamp",
                table: "Address",
                column: "CreateTimestamp");
        }
    }
}
