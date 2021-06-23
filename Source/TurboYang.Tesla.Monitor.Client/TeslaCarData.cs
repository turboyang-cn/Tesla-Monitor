using System;
using System.Text.Json.Serialization;

using NodaTime;

using TurboYang.Tesla.Monitor.Core.JsonConverters;
using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Client
{
    public record TeslaCarData : TeslaCar
    {
        [JsonPropertyName("user_id")]
        public Int32 UserId { get; init; }
        [JsonPropertyName("vehicle_config")]
        public TeslaCarConfig CarConfig { get; init; }
        [JsonPropertyName("vehicle_state")]
        public TeslaCarState CarState { get; init; }
        [JsonPropertyName("gui_settings")]
        public TeslaCarGuiSetting GuiSetting { get; init; }
        [JsonPropertyName("drive_state")]
        public TeslaCarDriveState DriveState { get; init; }
        [JsonPropertyName("climate_state")]
        public TeslaCarClimateState ClimateState { get; init; }
        [JsonPropertyName("charge_state")]
        public TeslaCarChargeState ChargeState { get; init; }

        public record TeslaCarConfig
        {
            [JsonPropertyName("can_accept_navigation_requests")]
            public Boolean CanAcceptNavigationRequest { get; init; }
            [JsonPropertyName("can_actuate_trunks")]
            public Boolean CanActuateTrunks { get; init; }
            [JsonPropertyName("car_special_type")]
            public String SpecialType { get; init; }
            [JsonPropertyName("car_type")]
            public CarType Type { get; init; }
            [JsonPropertyName("charge_port_type")]
            public String ChargePortType { get; init; }
            [JsonPropertyName("default_charge_to_max")]
            public Boolean IsDefaultChargeToMax { get; init; }
            [JsonPropertyName("ece_restrictions")]
            public Boolean IsEceRestrictions { get; init; }
            [JsonPropertyName("eu_vehicle")]
            public Boolean IsEuVehicle { get; init; }
            [JsonPropertyName("exterior_color")]
            public String ExteriorColor { get; init; }
            [JsonPropertyName("exterior_trim")]
            public String ExteriorTrim { get; init; }
            [JsonPropertyName("has_air_suspension")]
            public Boolean HasAirSuspension { get; init; }
            [JsonPropertyName("has_ludicrous_mode")]
            public Boolean HasLudicrousMode { get; init; }
            [JsonPropertyName("key_version")]
            public Int32 KeyVersion { get; init; }
            [JsonPropertyName("motorized_charge_port")]
            public Boolean HasMotorizedChargePort { get; init; }
            [JsonPropertyName("plg")]
            public Boolean HasPowerLiftGate { get; init; }
            [JsonPropertyName("rear_seat_heaters")]
            public Int32 RearSeatHeaters { get; init; }
            [JsonPropertyName("rear_seat_type")]
            public String RearSeatType { get; init; }
            [JsonPropertyName("rhd")]
            public Boolean IsRightHandDrive { get; init; }
            [JsonPropertyName("roof_color")]
            public String RoofColor { get; init; }
            [JsonPropertyName("seat_type")]
            public String SeatType { get; init; }
            [JsonPropertyName("spoiler_type")]
            public String SpoilerType { get; init; }
            [JsonPropertyName("sun_roof_installed")]
            public String SunRoofInstalled { get; init; }
            [JsonPropertyName("third_row_seats")]
            public String ThirdRowSeats { get; init; }
            [JsonPropertyName("use_range_badging")]
            public Boolean HasUseRangeBadging { get; init; }
            [JsonPropertyName("wheel_type")]
            public String WheelType { get; init; }
            [JsonPropertyName("timestamp")]
            [JsonConverter(typeof(UnixTimezoneToInstantConverter))]
            public Instant Timestamp { get; init; }
        }

        public record TeslaCarState
        {
            [JsonPropertyName("api_version")]
            public Int32 ApiVersion { get; init; }
            [JsonPropertyName("autopark_state_v2")]
            public String AutoparkState { get; init; }
            [JsonPropertyName("autopark_style")]
            public String AutoparkStyle { get; init; }
            [JsonPropertyName("calendar_supported")]
            public Boolean IsCalendarSupported { get; init; }
            [JsonPropertyName("car_version")]
            public String CarVersion { get; init; }
            [JsonPropertyName("center_display_state")]
            public CenterDisplayState CenterDisplayState { get; init; }
            [JsonPropertyName("df")]
            public SwitchState DriverFrontDoorState { get; init; }
            [JsonPropertyName("pf")]
            public SwitchState PassengerFrontDoorState { get; init; }
            [JsonPropertyName("dr")]
            public SwitchState DriverRearDoorState { get; init; }
            [JsonPropertyName("pr")]
            public SwitchState PassengerRearDoorState { get; init; }
            [JsonPropertyName("ft")]
            public SwitchState FrontTrunkDoorState { get; init; }
            [JsonPropertyName("rt")]
            public SwitchState RearTrunkDoorState { get; init; }
            [JsonPropertyName("fd_window")]
            public SwitchState DriverFrontWindowState { get; init; }
            [JsonPropertyName("fp_window")]
            public SwitchState PassengerFrontWindowState { get; init; }
            [JsonPropertyName("rd_window")]
            public SwitchState DriverRearWindowState { get; init; }
            [JsonPropertyName("rp_window")]
            public SwitchState PassengerRearWindowState { get; init; }
            [JsonPropertyName("is_user_present")]
            public Boolean IsUserPresent { get; init; }
            [JsonPropertyName("last_autopark_error")]
            public String LastAutoparkError { get; init; }
            [JsonPropertyName("locked")]
            public Boolean IsLocked { get; init; }
            [JsonPropertyName("media_state")]
            public TeslaCarMediaState MediaState { get; init; }
            [JsonPropertyName("notifications_supported")]
            public Boolean IsNotificationsSupported { get; init; }
            [JsonPropertyName("odometer")]
            [JsonConverter(typeof(MileToDistanceConverter))]
            public Distance Odometer { get; init; }
            [JsonPropertyName("parsed_calendar_supported")]
            public Boolean IsParsedCalendarSupported { get; init; }
            [JsonPropertyName("remote_start")]
            public Boolean IsRemoteStart { get; init; }
            [JsonPropertyName("remote_start_enabled")]
            public Boolean IsRemoteStartEnabled { get; init; }
            [JsonPropertyName("remote_start_supported")]
            public Boolean IsRemoteStartSupported { get; init; }
            [JsonPropertyName("sentry_mode")]
            public Boolean IsSentryMode { get; init; }
            [JsonPropertyName("sentry_mode_available")]
            public Boolean IsSentryModeAvailable { get; init; }
            [JsonPropertyName("smart_summon_available")]
            public Boolean IsSmartSummonAvailable { get; init; }
            [JsonPropertyName("valet_mode")]
            public Boolean IsValetMode { get; init; }
            [JsonPropertyName("valet_pin_needed")]
            public Boolean IsValetPinNeeded { get; init; }
            [JsonPropertyName("vehicle_name")]
            public String Name { get; init; }
            [JsonPropertyName("speed_limit_mode")]
            public TeslaCarSpeedLimitMode SpeedLimitMode { get; init; }
            [JsonPropertyName("software_update")]
            public TeslaCarSoftwareUpdate SoftwareUpdate { get; init; }
            [JsonPropertyName("timestamp")]
            [JsonConverter(typeof(UnixTimezoneToInstantConverter))]
            public Instant Timestamp { get; init; }

            public record TeslaCarMediaState
            {
                [JsonPropertyName("remote_control_enabled")]
                public Boolean IsRemoteControlEnabled { get; init; }
            }

            public record TeslaCarSpeedLimitMode
            {
                [JsonPropertyName("active")]
                public Boolean IsActive { get; init; }
                [JsonPropertyName("current_limit_mph")]
                [JsonConverter(typeof(MileToDistanceConverter))]
                public Distance CurrentLimitMph { get; init; }
                [JsonPropertyName("max_limit_mph")]
                [JsonConverter(typeof(MileToDistanceConverter))]
                public Distance MaxLimitMph { get; init; }
                [JsonPropertyName("min_limit_mph")]
                [JsonConverter(typeof(MileToDistanceConverter))]
                public Distance MinLimitMph { get; init; }
                [JsonPropertyName("pin_code_set")]
                public Boolean HasPinCodeSet { get; init; }
            }

            public record TeslaCarSoftwareUpdate
            {
                [JsonPropertyName("download_perc")]
                public Decimal DownloadPercentage { get; init; }
                [JsonPropertyName("install_perc")]
                public Decimal InstallPercentage { get; init; }
                [JsonPropertyName("expected_duration_sec")]
                public Int32 ExpectedDurationSeconds { get; init; }
                [JsonPropertyName("scheduled_time_ms")]
                [JsonConverter(typeof(UnixTimezoneToInstantConverter))]
                public Instant? ScheduledTimestamp { get; init; }
                [JsonPropertyName("status")]
                public SoftwareUpdateState Status { get; init; }
                [JsonPropertyName("warning_time_remaining_ms")]
                public Int32 WarningTimeRemaining_ms { get; init; }
                [JsonPropertyName("version")]
                public String Version { get; init; }
            }
        }

        public record TeslaCarGuiSetting
        {
            [JsonPropertyName("gui_24_hour_time")]
            public Boolean Is24HourTime { get; init; }
            [JsonPropertyName("show_range_units")]
            public Boolean IsShowRangeUnits { get; init; }
            [JsonPropertyName("gui_charge_rate_units")]
            public String ChargeRateUnits { get; init; }
            [JsonPropertyName("gui_distance_units")]
            public String DistanceUnits { get; init; }
            [JsonPropertyName("gui_range_display")]
            public String RangeDisplay { get; init; }
            [JsonPropertyName("gui_temperature_units")]
            public String TemperatureUnits { get; init; }
            [JsonPropertyName("timestamp")]
            [JsonConverter(typeof(UnixTimezoneToInstantConverter))]
            public Instant Timestamp { get; init; }
        }

        public record TeslaCarDriveState
        {
            [JsonPropertyName("corrected_latitude")]
            public Decimal CorrectedLatitude { get; init; }
            [JsonPropertyName("corrected_longitude")]
            public Decimal CorrectedLongitude { get; init; }
            [JsonPropertyName("gps_as_of")]
            [JsonConverter(typeof(UnixTimezoneToInstantConverter))]
            public Instant GpsTimestamp { get; init; }
            [JsonPropertyName("heading")]
            public Decimal Heading { get; init; }
            [JsonPropertyName("latitude")]
            public Decimal Latitude { get; init; }
            [JsonPropertyName("longitude")]
            public Decimal Longitude { get; init; }
            [JsonPropertyName("native_latitude")]
            public Decimal NativeLatitude { get; init; }
            [JsonPropertyName("native_longitude")]
            public Decimal NativeLongitude { get; init; }
            [JsonPropertyName("native_location_supported")]
            public Int32 NativeLocationSupported { get; init; }
            [JsonPropertyName("native_type")]
            public String NativeType { get; init; }
            [JsonPropertyName("power")]
            public Decimal Power { get; init; }
            [JsonPropertyName("shift_state")]
            public ShiftState? ShiftState { get; init; }
            [JsonPropertyName("speed")]
            [JsonConverter(typeof(MileToDistanceConverter))]
            public Distance Speed { get; init; }
            [JsonPropertyName("timestamp")]
            [JsonConverter(typeof(UnixTimezoneToInstantConverter))]
            public Instant Timestamp { get; init; }
        }

        public record TeslaCarClimateState
        {
            [JsonPropertyName("battery_heater")]
            public Boolean IsBatteryHeater { get; init; }
            [JsonPropertyName("battery_heater_no_power")]
            public Boolean? IsBatteryHeaterNoPower { get; init; }
            [JsonPropertyName("climate_keeper_mode")]
            public String ClimateKeeperMode { get; init; }
            [JsonPropertyName("defrost_mode")]
            public Int32 DefrostMode { get; init; }
            [JsonPropertyName("is_front_defroster_on")]
            public Boolean IsFrontDefrosterOn { get; init; }
            [JsonPropertyName("is_rear_defroster_on")]
            public Boolean IsRearDefrosterOn { get; init; }
            [JsonPropertyName("fan_status")]
            public Int32 FanStatus { get; init; }
            [JsonPropertyName("driver_temp_setting")]
            [JsonConverter(typeof(CelsiusToTemperatureConverter))]
            public Temperature DriverTemperatureSetting { get; init; }
            [JsonPropertyName("passenger_temp_setting")]
            [JsonConverter(typeof(CelsiusToTemperatureConverter))]
            public Temperature PassengerTemperatureSetting { get; init; }
            [JsonPropertyName("inside_temp")]
            [JsonConverter(typeof(CelsiusToTemperatureConverter))]
            public Temperature InsideTemperature { get; init; }
            [JsonPropertyName("outside_temp")]
            [JsonConverter(typeof(CelsiusToTemperatureConverter))]
            public Temperature OutsideTemperature { get; init; }
            [JsonPropertyName("is_auto_conditioning_on")]
            public Boolean IsAutoConditioningOn { get; init; }
            [JsonPropertyName("is_climate_on")]
            public Boolean IsClimateOn { get; init; }
            [JsonPropertyName("is_preconditioning")]
            public Boolean IsPreconditioning { get; init; }
            [JsonPropertyName("left_temp_direction")]
            public Int32 DriverTemperatureDirection { get; init; }
            [JsonPropertyName("right_temp_direction")]
            public Int32 PassengerTemperatureDirection { get; init; }
            [JsonPropertyName("max_avail_temp")]
            [JsonConverter(typeof(CelsiusToTemperatureConverter))]
            public Temperature MaxAvailTemperature { get; init; }
            [JsonPropertyName("min_avail_temp")]
            [JsonConverter(typeof(CelsiusToTemperatureConverter))]
            public Temperature MinAvailTemperature { get; init; }
            [JsonPropertyName("remote_heater_control_enabled")]
            public Boolean IsRemoteHeaterControlEnabled { get; init; }
            [JsonPropertyName("seat_heater_left")]
            public Int32 DriverSeatHeater { get; init; }
            [JsonPropertyName("seat_heater_right")]
            public Int32 PassengerSeatHeater { get; init; }
            [JsonPropertyName("side_mirror_heaters")]
            public Boolean IsSideMirrorHeater { get; init; }
            [JsonPropertyName("wiper_blade_heater")]
            public Boolean IsWiperBladeHeater { get; init; }
            [JsonPropertyName("timestamp")]
            [JsonConverter(typeof(UnixTimezoneToInstantConverter))]
            public Instant Timestamp { get; init; }
        }

        public record TeslaCarChargeState
        {
            [JsonPropertyName("battery_heater_on")]
            public Boolean IsBatteryHeaterOn { get; init; }
            [JsonPropertyName("battery_level")]
            public Decimal BatteryLevel { get; init; }
            [JsonPropertyName("battery_range")]
            [JsonConverter(typeof(MileToDistanceConverter))]
            public Distance BatteryRange { get; init; }
            [JsonPropertyName("est_battery_range")]
            [JsonConverter(typeof(MileToDistanceConverter))]
            public Distance RatedBatteryRange { get; init; }
            [JsonPropertyName("ideal_battery_range")]
            [JsonConverter(typeof(MileToDistanceConverter))]
            public Distance IdealBatteryRange { get; init; }
            [JsonPropertyName("charge_current_request")]
            public Int32 ChargeCurrentRequest { get; init; }
            [JsonPropertyName("charge_current_request_max")]
            public Int32 MaxChargeCurrentRequest { get; init; }
            [JsonPropertyName("charge_enable_request")]
            public Boolean IsChargeEnableRequest { get; init; }
            [JsonPropertyName("charge_energy_added")]
            public Decimal ChargeEnergyAdded { get; init; }
            [JsonPropertyName("charge_limit_soc")]
            public Int32 ChargeLimitSoc { get; init; }
            [JsonPropertyName("charge_limit_soc_max")]
            public Int32 MaxChargeLimitSoc { get; init; }
            [JsonPropertyName("charge_limit_soc_min")]
            public Int32 MinChargeLimitSoc { get; init; }
            [JsonPropertyName("charge_limit_soc_std")]
            public Int32 StandardChargeLimitSoc { get; init; }
            [JsonPropertyName("charge_miles_added_ideal")]
            [JsonConverter(typeof(MileToDistanceConverter))]
            public Distance ChargeMilesAddedIdeal { get; init; }
            [JsonPropertyName("charge_miles_added_rated")]
            [JsonConverter(typeof(MileToDistanceConverter))]
            public Distance ChargeMilesAddedRated { get; init; }
            [JsonPropertyName("charge_port_cold_weather_mode")]
            public Boolean IsChargePortColdWeatherMode { get; init; }
            [JsonPropertyName("charge_port_door_open")]
            public Boolean IsChargePortDoorOpen { get; init; }
            [JsonPropertyName("charge_port_latch")]
            public String ChargePortLatch { get; init; }
            [JsonPropertyName("charge_rate")]
            public Decimal ChargeRate { get; init; }
            [JsonPropertyName("charge_to_max_range")]
            public Boolean IsChargeToMaxRange { get; init; }
            [JsonPropertyName("charger_pilot_current")]
            public Int32 ChargerPilotCurrent { get; init; }
            [JsonPropertyName("charger_phases")]
            public Int32? ChargerPhases { get; init; }
            [JsonPropertyName("charger_power")]
            public Int32 ChargerPower { get; init; }
            [JsonPropertyName("charger_actual_current")]
            public Int32 ChargerActualCurrent { get; init; }
            [JsonPropertyName("charger_voltage")]
            public Int32 ChargerVoltage { get; init; }
            [JsonPropertyName("charging_state")]
            public String ChargingState { get; init; }
            [JsonPropertyName("conn_charge_cable")]
            public String ChargeCable { get; init; }
            [JsonPropertyName("fast_charger_brand")]
            public String FastChargerBrand { get; init; }
            [JsonPropertyName("fast_charger_type")]
            public String FastChargerType { get; init; }
            [JsonPropertyName("fast_charger_present")]
            public Boolean IsFastChargerPresent { get; init; }
            [JsonPropertyName("managed_charging_active")]
            public Boolean IsManagedChargingActive { get; init; }
            [JsonPropertyName("managed_charging_user_canceled")]
            public Boolean IsmanagedChargingUserCanceled { get; init; }
            [JsonPropertyName("max_range_charge_counter")]
            public Int32 MaxRangeChargeCounter { get; init; }
            [JsonPropertyName("minutes_to_full_charge")]
            public Int32 FullChageMinutes { get; init; }
            [JsonPropertyName("time_to_full_charge")]
            public Decimal FullChageHours { get; init; }
            [JsonPropertyName("scheduled_charging_pending")]
            public Boolean IsScheduledChargingPending { get; init; }
            [JsonPropertyName("scheduled_charging_start_time")]
            [JsonConverter(typeof(UnixTimezoneToInstantConverter))]
            public Instant? ScheduledChargingStartTime { get; init; }
            [JsonPropertyName("scheduled_departure_time")]
            [JsonConverter(typeof(UnixTimezoneToInstantConverter))]
            public Instant? ScheduledDepartureTime { get; init; }
            [JsonPropertyName("trip_charging")]
            public Boolean IsTripCharging { get; init; }
            [JsonPropertyName("usable_battery_level")]
            public Int32 UsableBatteryLevel { get; init; }
            [JsonPropertyName("timestamp")]
            [JsonConverter(typeof(UnixTimezoneToInstantConverter))]
            public Instant Timestamp { get; init; }
        }
    }
}
