using Microsoft.EntityFrameworkCore.Migrations;

namespace TurboYang.Tesla.Monitor.Database.Migrations
{
    public partial class _202107080001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChargeCable",
                table: "Snapshot",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FastChargerBrand",
                table: "Snapshot",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FastChargerType",
                table: "Snapshot",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFastChargerPresent",
                table: "Snapshot",
                type: "boolean",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChargeCable",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "FastChargerBrand",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "FastChargerType",
                table: "Snapshot");

            migrationBuilder.DropColumn(
                name: "IsFastChargerPresent",
                table: "Snapshot");
        }
    }
}
