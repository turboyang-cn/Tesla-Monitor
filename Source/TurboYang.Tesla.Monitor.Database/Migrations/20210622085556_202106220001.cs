using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace TurboYang.Tesla.Monitor.Database.Migrations
{
    public partial class _202106220001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AddressId",
                table: "StandBy",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndAddressId",
                table: "Driving",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartAddressId",
                table: "Driving",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AddressId",
                table: "Charging",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Address",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Location = table.Column<Point>(type: "geography (point)", nullable: false),
                    Radius = table.Column<decimal>(type: "numeric", nullable: false, defaultValue: 30m),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Postcode = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    County = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    District = table.Column<string>(type: "text", nullable: true),
                    Village = table.Column<string>(type: "text", nullable: true),
                    Road = table.Column<string>(type: "text", nullable: true),
                    Building = table.Column<string>(type: "text", nullable: true),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Address", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StandBy_AddressId",
                table: "StandBy",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Driving_EndAddressId",
                table: "Driving",
                column: "EndAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Driving_StartAddressId",
                table: "Driving",
                column: "StartAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Charging_AddressId",
                table: "Charging",
                column: "AddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Address_CreateTimestamp",
                table: "Address",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Address_OpenId",
                table: "Address",
                column: "OpenId");

            migrationBuilder.AddForeignKey(
                name: "FK_Charging_Address_AddressId",
                table: "Charging",
                column: "AddressId",
                principalTable: "Address",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Driving_Address_EndAddressId",
                table: "Driving",
                column: "EndAddressId",
                principalTable: "Address",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Driving_Address_StartAddressId",
                table: "Driving",
                column: "StartAddressId",
                principalTable: "Address",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StandBy_Address_AddressId",
                table: "StandBy",
                column: "AddressId",
                principalTable: "Address",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Charging_Address_AddressId",
                table: "Charging");

            migrationBuilder.DropForeignKey(
                name: "FK_Driving_Address_EndAddressId",
                table: "Driving");

            migrationBuilder.DropForeignKey(
                name: "FK_Driving_Address_StartAddressId",
                table: "Driving");

            migrationBuilder.DropForeignKey(
                name: "FK_StandBy_Address_AddressId",
                table: "StandBy");

            migrationBuilder.DropTable(
                name: "Address");

            migrationBuilder.DropIndex(
                name: "IX_StandBy_AddressId",
                table: "StandBy");

            migrationBuilder.DropIndex(
                name: "IX_Driving_EndAddressId",
                table: "Driving");

            migrationBuilder.DropIndex(
                name: "IX_Driving_StartAddressId",
                table: "Driving");

            migrationBuilder.DropIndex(
                name: "IX_Charging_AddressId",
                table: "Charging");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "StandBy");

            migrationBuilder.DropColumn(
                name: "EndAddressId",
                table: "Driving");

            migrationBuilder.DropColumn(
                name: "StartAddressId",
                table: "Driving");

            migrationBuilder.DropColumn(
                name: "AddressId",
                table: "Charging");
        }
    }
}
