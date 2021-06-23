using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using NodaTime;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Database.Migrations
{
    public partial class _202106210001 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:CarState", "Online,Asleep,Offline,Driving,Charging")
                .Annotation("Npgsql:Enum:CarType", "Model3")
                .Annotation("Npgsql:Enum:ShiftState", "P,D,N,R")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .Annotation("Npgsql:PostgresExtension:uuid-ossp", ",,");

            migrationBuilder.CreateTable(
                name: "Token",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: true),
                    AccessToken = table.Column<string>(type: "text", nullable: true),
                    RefreshToken = table.Column<string>(type: "text", nullable: true),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Token", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Car",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CarId = table.Column<string>(type: "text", nullable: true),
                    VehicleId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<CarType>(type: "\"CarType\"", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Vin = table.Column<string>(type: "text", nullable: true),
                    ExteriorColor = table.Column<string>(type: "text", nullable: true),
                    WheelType = table.Column<string>(type: "text", nullable: true),
                    TokenId = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Car", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Car_Token_TokenId",
                        column: x => x.TokenId,
                        principalTable: "Token",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarSetting",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SamplingRate = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    IsSamplingCompression = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    TryAsleepDelay = table.Column<int>(type: "integer", nullable: false, defaultValue: 300),
                    CarId = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarSetting", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarSetting_Car_CarId",
                        column: x => x.CarId,
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Charging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartTimestamp = table.Column<Instant>(type: "timestamp", nullable: true),
                    EndTimestamp = table.Column<Instant>(type: "timestamp", nullable: true),
                    StartBatteryLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    EndBatteryLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    StartPower = table.Column<decimal>(type: "numeric", nullable: true),
                    EndPower = table.Column<decimal>(type: "numeric", nullable: true),
                    StartIdealBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    EndIdealBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    StartRatedBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    EndRatedBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    Location = table.Column<Point>(type: "geography (point)", nullable: true),
                    Elevation = table.Column<decimal>(type: "numeric", nullable: true),
                    Heading = table.Column<decimal>(type: "numeric", nullable: true),
                    Odometer = table.Column<decimal>(type: "numeric", nullable: true),
                    IsFastChargerPresent = table.Column<bool>(type: "boolean", nullable: true),
                    ChargeCable = table.Column<string>(type: "text", nullable: true),
                    FastChargerBrand = table.Column<string>(type: "text", nullable: true),
                    FastChargerType = table.Column<string>(type: "text", nullable: true),
                    ChargeEnergyAdded = table.Column<decimal>(type: "numeric", nullable: true),
                    ChargeEnergyUsed = table.Column<decimal>(type: "numeric", nullable: true),
                    Efficiency = table.Column<decimal>(type: "numeric", nullable: true),
                    Duration = table.Column<decimal>(type: "numeric", nullable: true),
                    CarId = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charging", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Charging_Car_CarId",
                        column: x => x.CarId,
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Driving",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartTimestamp = table.Column<Instant>(type: "timestamp", nullable: true),
                    EndTimestamp = table.Column<Instant>(type: "timestamp", nullable: true),
                    StartLocation = table.Column<Point>(type: "geography (point)", nullable: true),
                    EndLocation = table.Column<Point>(type: "geography (point)", nullable: true),
                    StartBatteryLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    EndBatteryLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    StartPower = table.Column<decimal>(type: "numeric", nullable: true),
                    EndPower = table.Column<decimal>(type: "numeric", nullable: true),
                    StartIdealBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    EndIdealBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    StartRatedBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    EndRatedBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    StartOdometer = table.Column<decimal>(type: "numeric", nullable: true),
                    EndOdometer = table.Column<decimal>(type: "numeric", nullable: true),
                    Distance = table.Column<decimal>(type: "numeric", nullable: true),
                    Duration = table.Column<decimal>(type: "numeric", nullable: true),
                    SpeedAverage = table.Column<decimal>(type: "numeric", nullable: true),
                    CarId = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Driving", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Driving_Car_CarId",
                        column: x => x.CarId,
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StandBy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartTimestamp = table.Column<Instant>(type: "timestamp", nullable: true),
                    EndTimestamp = table.Column<Instant>(type: "timestamp", nullable: true),
                    StartBatteryLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    EndBatteryLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    StartPower = table.Column<decimal>(type: "numeric", nullable: true),
                    EndPower = table.Column<decimal>(type: "numeric", nullable: true),
                    StartIdealBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    EndIdealBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    StartRatedBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    EndRatedBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    Location = table.Column<Point>(type: "geography (point)", nullable: true),
                    Elevation = table.Column<decimal>(type: "numeric", nullable: true),
                    Heading = table.Column<decimal>(type: "numeric", nullable: true),
                    Odometer = table.Column<decimal>(type: "numeric", nullable: true),
                    Duration = table.Column<decimal>(type: "numeric", nullable: true),
                    OnlineRatio = table.Column<decimal>(type: "numeric", nullable: true),
                    CarId = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandBy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandBy_Car_CarId",
                        column: x => x.CarId,
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "State",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    State = table.Column<CarState>(type: "\"CarState\"", nullable: false),
                    StartTimestamp = table.Column<Instant>(type: "timestamp", nullable: false),
                    EndTimestamp = table.Column<Instant>(type: "timestamp", nullable: true),
                    CarId = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_State", x => x.Id);
                    table.ForeignKey(
                        name: "FK_State_Car_CarId",
                        column: x => x.CarId,
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChargingSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IsFastChargerPresent = table.Column<bool>(type: "boolean", nullable: true),
                    ChargeCable = table.Column<string>(type: "text", nullable: true),
                    FastChargerBrand = table.Column<string>(type: "text", nullable: true),
                    FastChargerType = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<Point>(type: "geography (point)", nullable: true),
                    Elevation = table.Column<decimal>(type: "numeric", nullable: true),
                    Odometer = table.Column<decimal>(type: "numeric", nullable: true),
                    Heading = table.Column<decimal>(type: "numeric", nullable: true),
                    BatteryLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    IdealBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    RatedBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    OutsideTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                    InsideTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                    DriverTemperatureSetting = table.Column<decimal>(type: "numeric", nullable: true),
                    PassengerTemperatureSetting = table.Column<decimal>(type: "numeric", nullable: true),
                    DriverSeatHeater = table.Column<int>(type: "integer", nullable: true),
                    PassengerSeatHeater = table.Column<int>(type: "integer", nullable: true),
                    FanStatus = table.Column<int>(type: "integer", nullable: true),
                    IsSideMirrorHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsWiperBladeHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsFrontDefrosterOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsRearDefrosterOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsClimateOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsBatteryHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsBatteryHeaterOn = table.Column<bool>(type: "boolean", nullable: true),
                    ChargeEnergyAdded = table.Column<decimal>(type: "numeric", nullable: true),
                    ChargerPhases = table.Column<int>(type: "integer", nullable: true),
                    ChargerPilotCurrent = table.Column<int>(type: "integer", nullable: true),
                    ChargerActualCurrent = table.Column<int>(type: "integer", nullable: true),
                    ChargerPower = table.Column<int>(type: "integer", nullable: true),
                    ChargerVoltage = table.Column<int>(type: "integer", nullable: true),
                    ChargeRate = table.Column<decimal>(type: "numeric", nullable: true),
                    ChargingId = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChargingSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChargingSnapshot_Charging_ChargingId",
                        column: x => x.ChargingId,
                        principalTable: "Charging",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DrivingSnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Location = table.Column<Point>(type: "geography (point)", nullable: true),
                    Elevation = table.Column<decimal>(type: "numeric", nullable: true),
                    Speed = table.Column<decimal>(type: "numeric", nullable: true),
                    Heading = table.Column<decimal>(type: "numeric", nullable: true),
                    ShiftState = table.Column<ShiftState>(type: "\"ShiftState\"", nullable: true),
                    Power = table.Column<decimal>(type: "numeric", nullable: true),
                    Odometer = table.Column<decimal>(type: "numeric", nullable: true),
                    BatteryLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    IdealBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    RatedBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    OutsideTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                    InsideTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                    DriverTemperatureSetting = table.Column<decimal>(type: "numeric", nullable: true),
                    PassengerTemperatureSetting = table.Column<decimal>(type: "numeric", nullable: true),
                    DriverSeatHeater = table.Column<int>(type: "integer", nullable: true),
                    PassengerSeatHeater = table.Column<int>(type: "integer", nullable: true),
                    FanStatus = table.Column<int>(type: "integer", nullable: true),
                    IsSideMirrorHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsWiperBladeHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsFrontDefrosterOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsRearDefrosterOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsClimateOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsBatteryHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsBatteryHeaterOn = table.Column<bool>(type: "boolean", nullable: true),
                    DrivingId = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrivingSnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DrivingSnapshot_Driving_DrivingId",
                        column: x => x.DrivingId,
                        principalTable: "Driving",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StandBySnapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Location = table.Column<Point>(type: "geography (point)", nullable: true),
                    Elevation = table.Column<decimal>(type: "numeric", nullable: true),
                    Odometer = table.Column<decimal>(type: "numeric", nullable: true),
                    Heading = table.Column<decimal>(type: "numeric", nullable: true),
                    Power = table.Column<decimal>(type: "numeric", nullable: true),
                    BatteryLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    IdealBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    RatedBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    OutsideTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                    InsideTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                    DriverTemperatureSetting = table.Column<decimal>(type: "numeric", nullable: true),
                    PassengerTemperatureSetting = table.Column<decimal>(type: "numeric", nullable: true),
                    DriverSeatHeater = table.Column<int>(type: "integer", nullable: true),
                    PassengerSeatHeater = table.Column<int>(type: "integer", nullable: true),
                    FanStatus = table.Column<int>(type: "integer", nullable: true),
                    IsSideMirrorHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsWiperBladeHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsFrontDefrosterOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsRearDefrosterOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsClimateOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsBatteryHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsBatteryHeaterOn = table.Column<bool>(type: "boolean", nullable: true),
                    StandById = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StandBySnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StandBySnapshot_StandBy_StandById",
                        column: x => x.StandById,
                        principalTable: "StandBy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Snapshot",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:IdentitySequenceOptions", "'10000000', '1', '', '', 'False', '1'")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Location = table.Column<Point>(type: "geography (point)", nullable: true),
                    Elevation = table.Column<decimal>(type: "numeric", nullable: true),
                    Speed = table.Column<decimal>(type: "numeric", nullable: true),
                    Heading = table.Column<decimal>(type: "numeric", nullable: true),
                    ShiftState = table.Column<ShiftState>(type: "\"ShiftState\"", nullable: true),
                    Power = table.Column<decimal>(type: "numeric", nullable: true),
                    Odometer = table.Column<decimal>(type: "numeric", nullable: true),
                    BatteryLevel = table.Column<decimal>(type: "numeric", nullable: true),
                    IdealBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    RatedBatteryRange = table.Column<decimal>(type: "numeric", nullable: true),
                    OutsideTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                    InsideTemperature = table.Column<decimal>(type: "numeric", nullable: true),
                    DriverTemperatureSetting = table.Column<decimal>(type: "numeric", nullable: true),
                    PassengerTemperatureSetting = table.Column<decimal>(type: "numeric", nullable: true),
                    DriverSeatHeater = table.Column<int>(type: "integer", nullable: true),
                    PassengerSeatHeater = table.Column<int>(type: "integer", nullable: true),
                    FanStatus = table.Column<int>(type: "integer", nullable: true),
                    IsSideMirrorHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsWiperBladeHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsFrontDefrosterOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsRearDefrosterOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsClimateOn = table.Column<bool>(type: "boolean", nullable: true),
                    IsBatteryHeater = table.Column<bool>(type: "boolean", nullable: true),
                    IsBatteryHeaterOn = table.Column<bool>(type: "boolean", nullable: true),
                    ChargeEnergyAdded = table.Column<decimal>(type: "numeric", nullable: true),
                    ChargeEnergyUsed = table.Column<decimal>(type: "numeric", nullable: true),
                    ChargerPhases = table.Column<int>(type: "integer", nullable: true),
                    ChargerPilotCurrent = table.Column<int>(type: "integer", nullable: true),
                    ChargerActualCurrent = table.Column<int>(type: "integer", nullable: true),
                    ChargerPower = table.Column<int>(type: "integer", nullable: true),
                    ChargerVoltage = table.Column<int>(type: "integer", nullable: true),
                    ChargeRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CarId = table.Column<int>(type: "integer", nullable: false),
                    StateId = table.Column<int>(type: "integer", nullable: false),
                    OpenId = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                    CreateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    UpdateBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "System"),
                    CreateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())"),
                    UpdateTimestamp = table.Column<Instant>(type: "timestamp", nullable: false, defaultValueSql: "timezone('utc'::text, now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Snapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Snapshot_Car_CarId",
                        column: x => x.CarId,
                        principalTable: "Car",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Snapshot_State_StateId",
                        column: x => x.StateId,
                        principalTable: "State",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Car_CreateTimestamp",
                table: "Car",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Car_OpenId",
                table: "Car",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_Car_TokenId",
                table: "Car",
                column: "TokenId");

            migrationBuilder.CreateIndex(
                name: "IX_CarSetting_CarId",
                table: "CarSetting",
                column: "CarId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CarSetting_CreateTimestamp",
                table: "CarSetting",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CarSetting_OpenId",
                table: "CarSetting",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_Charging_CarId",
                table: "Charging",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Charging_CreateTimestamp",
                table: "Charging",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Charging_OpenId",
                table: "Charging",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSnapshot_ChargingId",
                table: "ChargingSnapshot",
                column: "ChargingId");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSnapshot_CreateTimestamp",
                table: "ChargingSnapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ChargingSnapshot_OpenId",
                table: "ChargingSnapshot",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_Driving_CarId",
                table: "Driving",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Driving_CreateTimestamp",
                table: "Driving",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Driving_OpenId",
                table: "Driving",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_DrivingSnapshot_CreateTimestamp",
                table: "DrivingSnapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_DrivingSnapshot_DrivingId",
                table: "DrivingSnapshot",
                column: "DrivingId");

            migrationBuilder.CreateIndex(
                name: "IX_DrivingSnapshot_OpenId",
                table: "DrivingSnapshot",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_CarId",
                table: "Snapshot",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_CreateTimestamp",
                table: "Snapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_OpenId",
                table: "Snapshot",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_Snapshot_StateId",
                table: "Snapshot",
                column: "StateId");

            migrationBuilder.CreateIndex(
                name: "IX_StandBy_CarId",
                table: "StandBy",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_StandBy_CreateTimestamp",
                table: "StandBy",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_StandBy_OpenId",
                table: "StandBy",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_StandBySnapshot_CreateTimestamp",
                table: "StandBySnapshot",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_StandBySnapshot_OpenId",
                table: "StandBySnapshot",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_StandBySnapshot_StandById",
                table: "StandBySnapshot",
                column: "StandById");

            migrationBuilder.CreateIndex(
                name: "IX_State_CarId",
                table: "State",
                column: "CarId");

            migrationBuilder.CreateIndex(
                name: "IX_State_CreateTimestamp",
                table: "State",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_State_OpenId",
                table: "State",
                column: "OpenId");

            migrationBuilder.CreateIndex(
                name: "IX_Token_CreateTimestamp",
                table: "Token",
                column: "CreateTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Token_OpenId",
                table: "Token",
                column: "OpenId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarSetting");

            migrationBuilder.DropTable(
                name: "ChargingSnapshot");

            migrationBuilder.DropTable(
                name: "DrivingSnapshot");

            migrationBuilder.DropTable(
                name: "Snapshot");

            migrationBuilder.DropTable(
                name: "StandBySnapshot");

            migrationBuilder.DropTable(
                name: "Charging");

            migrationBuilder.DropTable(
                name: "Driving");

            migrationBuilder.DropTable(
                name: "State");

            migrationBuilder.DropTable(
                name: "StandBy");

            migrationBuilder.DropTable(
                name: "Car");

            migrationBuilder.DropTable(
                name: "Token");
        }
    }
}
