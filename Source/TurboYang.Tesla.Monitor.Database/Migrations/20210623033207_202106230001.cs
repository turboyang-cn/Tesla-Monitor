using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

namespace TurboYang.Tesla.Monitor.Database.Migrations
{
    public partial class _202106230001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "Timestamp",
                table: "StandBySnapshot",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "Timestamp",
                table: "Snapshot",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "Timestamp",
                table: "DrivingSnapshot",
                type: "timestamp",
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "Timestamp",
                table: "ChargingSnapshot",
                type: "timestamp",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "StandBySnapshot");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "DrivingSnapshot");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "ChargingSnapshot");
        }
    }
}
