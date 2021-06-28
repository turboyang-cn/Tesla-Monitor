using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Database.Migrations
{
    public partial class _202106280001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "StandBy");

            migrationBuilder.DropColumn(
                name: "EndLocation",
                table: "Driving");

            migrationBuilder.DropColumn(
                name: "StartLocation",
                table: "Driving");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Charging");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:CarState", "Online,Asleep,Offline,Driving,Charging")
                .Annotation("Npgsql:Enum:CarType", "Model3")
                .Annotation("Npgsql:Enum:FirewareState", "Pending,Updated")
                .Annotation("Npgsql:Enum:ShiftState", "P,D,N,R")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .OldAnnotation("Npgsql:Enum:CarState", "Online,Asleep,Offline,Driving,Charging")
                .OldAnnotation("Npgsql:Enum:CarType", "Model3")
                .OldAnnotation("Npgsql:Enum:ShiftState", "P,D,N,R")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.AddColumn<decimal>(
                name: "FullPower",
                table: "CarSetting",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Fireware",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Version = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<Instant>(type: "timestamp", nullable: false),
                    State = table.Column<FirewareState>(type: "\"FirewareState\"", nullable: false, defaultValue: FirewareState.Pending),
                    CarId = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fireware", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fireware_Car_CarId",
                        column: x => x.CarId,
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Fireware_CarId",
                table: "Fireware",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Fireware_OpenId",
                table: "Fireware",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_Fireware_Timestamp",
                table: "Fireware",
                column: "Timestamp");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fireware");

            migrationBuilder.DropColumn(
                name: "FullPower",
                table: "CarSetting");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:CarState", "Online,Asleep,Offline,Driving,Charging")
                .Annotation("Npgsql:Enum:CarType", "Model3")
                .Annotation("Npgsql:Enum:ShiftState", "P,D,N,R")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,")
                .OldAnnotation("Npgsql:Enum:CarState", "Online,Asleep,Offline,Driving,Charging")
                .OldAnnotation("Npgsql:Enum:CarType", "Model3")
                .OldAnnotation("Npgsql:Enum:FirewareState", "Pending,Updated")
                .OldAnnotation("Npgsql:Enum:ShiftState", "P,D,N,R")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "StandBy",
                type: "geography (point)",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "EndLocation",
                table: "Driving",
                type: "geography (point)",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "StartLocation",
                table: "Driving",
                type: "geography (point)",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Charging",
                type: "geography (point)",
                nullable: true);
        }
    }
}
