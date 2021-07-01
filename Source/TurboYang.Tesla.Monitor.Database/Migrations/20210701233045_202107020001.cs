using Microsoft.EntityFrameworkCore.Migrations;

namespace TurboYang.Tesla.Monitor.Database.Migrations
{
    public partial class _202107020001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Token_CreateTimestamp",
                table: "Token",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Token_UpdateTimestamp",
                table: "Token",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_State_CreateTimestamp",
                table: "State",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_State_UpdateTimestamp",
                table: "State",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_StandBySnapshot_CreateTimestamp",
                table: "StandBySnapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_StandBySnapshot_UpdateTimestamp",
                table: "StandBySnapshot",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_StandBy_CreateTimestamp",
                table: "StandBy",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_StandBy_UpdateTimestamp",
                table: "StandBy",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_CreateTimestamp",
                table: "Snapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_UpdateTimestamp",
                table: "Snapshot",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Fireware_CreateTimestamp",
                table: "Fireware",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Fireware_UpdateTimestamp",
                table: "Fireware",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DrivingSnapshot_CreateTimestamp",
                table: "DrivingSnapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DrivingSnapshot_UpdateTimestamp",
                table: "DrivingSnapshot",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Driving_CreateTimestamp",
                table: "Driving",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Driving_UpdateTimestamp",
                table: "Driving",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSnapshot_CreateTimestamp",
                table: "ChargingSnapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSnapshot_UpdateTimestamp",
                table: "ChargingSnapshot",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Charging_CreateTimestamp",
                table: "Charging",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Charging_UpdateTimestamp",
                table: "Charging",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CarSetting_CreateTimestamp",
                table: "CarSetting",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CarSetting_UpdateTimestamp",
                table: "CarSetting",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Car_CreateTimestamp",
                table: "Car",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Car_UpdateTimestamp",
                table: "Car",
                column: "UpdateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Address_CreateTimestamp",
                table: "Address",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Address_UpdateTimestamp",
                table: "Address",
                column: "UpdateTimestamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Token_CreateTimestamp",
                table: "Token");

            migrationBuilder.DropIndex(
                name: "IX_Token_UpdateTimestamp",
                table: "Token");

            migrationBuilder.DropIndex(
                name: "IX_State_CreateTimestamp",
                table: "State");

            migrationBuilder.DropIndex(
                name: "IX_State_UpdateTimestamp",
                table: "State");

            migrationBuilder.DropIndex(
                name: "IX_StandBySnapshot_CreateTimestamp",
                table: "StandBySnapshot");

            migrationBuilder.DropIndex(
                name: "IX_StandBySnapshot_UpdateTimestamp",
                table: "StandBySnapshot");

            migrationBuilder.DropIndex(
                name: "IX_StandBy_CreateTimestamp",
                table: "StandBy");

            migrationBuilder.DropIndex(
                name: "IX_StandBy_UpdateTimestamp",
                table: "StandBy");

            migrationBuilder.DropIndex(
                name: "IX_Snapshot_CreateTimestamp",
                table: "Snapshot");

            migrationBuilder.DropIndex(
                name: "IX_Snapshot_UpdateTimestamp",
                table: "Snapshot");

            migrationBuilder.DropIndex(
                name: "IX_Fireware_CreateTimestamp",
                table: "Fireware");

            migrationBuilder.DropIndex(
                name: "IX_Fireware_UpdateTimestamp",
                table: "Fireware");

            migrationBuilder.DropIndex(
                name: "IX_DrivingSnapshot_CreateTimestamp",
                table: "DrivingSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_DrivingSnapshot_UpdateTimestamp",
                table: "DrivingSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_Driving_CreateTimestamp",
                table: "Driving");

            migrationBuilder.DropIndex(
                name: "IX_Driving_UpdateTimestamp",
                table: "Driving");

            migrationBuilder.DropIndex(
                name: "IX_ChargingSnapshot_CreateTimestamp",
                table: "ChargingSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_ChargingSnapshot_UpdateTimestamp",
                table: "ChargingSnapshot");

            migrationBuilder.DropIndex(
                name: "IX_Charging_CreateTimestamp",
                table: "Charging");

            migrationBuilder.DropIndex(
                name: "IX_Charging_UpdateTimestamp",
                table: "Charging");

            migrationBuilder.DropIndex(
                name: "IX_CarSetting_CreateTimestamp",
                table: "CarSetting");

            migrationBuilder.DropIndex(
                name: "IX_CarSetting_UpdateTimestamp",
                table: "CarSetting");

            migrationBuilder.DropIndex(
                name: "IX_Car_CreateTimestamp",
                table: "Car");

            migrationBuilder.DropIndex(
                name: "IX_Car_UpdateTimestamp",
                table: "Car");

            migrationBuilder.DropIndex(
                name: "IX_Address_CreateTimestamp",
                table: "Address");

            migrationBuilder.DropIndex(
                name: "IX_Address_UpdateTimestamp",
                table: "Address");
        }
    }
}
