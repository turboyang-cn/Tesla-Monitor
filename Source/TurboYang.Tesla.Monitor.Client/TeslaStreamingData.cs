using System;
using System.Collections.Generic;
using System.Linq;

using NodaTime;

using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Client
{
    public record TeslaStreamingData
    {
        public TeslaStreamingData(String data)
        {
            try
            {
                List<String> values = data.Split(",").ToList();

                if (Int64.TryParse(values[0], out Int64 timestamp))
                {
                    Instant BaseDateTime = Instant.FromUtc(1970, 1, 1, 0, 0, 0);

                    if (timestamp >= 1000000000000)
                    {
                        Timestamp = BaseDateTime.Plus(Duration.FromMilliseconds(timestamp));
                    }
                    else
                    {
                        Timestamp = BaseDateTime.Plus(Duration.FromSeconds(timestamp));
                    }
                }

                if (Decimal.TryParse(values[1], out Decimal speed))
                {
                    Speed = new Distance()
                    {
                        Mile = speed,
                    };
                }
                if (Decimal.TryParse(values[2], out Decimal odometer))
                {
                    Odometer = new Distance()
                    {
                        Mile = odometer,
                    };
                }
                if (Decimal.TryParse(values[3], out Decimal batteryLevel))
                {
                    BatteryLevel = batteryLevel;
                }
                if (Decimal.TryParse(values[4], out Decimal elevation))
                {
                    Elevation = elevation;
                }
                if (Decimal.TryParse(values[5], out Decimal estimateHeading))
                {
                    EstimateHeading = estimateHeading;
                }
                if (Decimal.TryParse(values[6], out Decimal latitude))
                {
                    Latitude = latitude;
                }
                if (Decimal.TryParse(values[7], out Decimal longitude))
                {
                    Longitude = longitude;
                }
                if (Decimal.TryParse(values[8], out Decimal power))
                {
                    Power = power;
                }
                if (Decimal.TryParse(values[10], out Decimal batteryRange))
                {
                    BatteryRange = new Distance()
                    {
                        Mile = batteryRange,
                    };
                }
                if (Decimal.TryParse(values[11], out Decimal estimateBatteryRange))
                {
                    EstimateBatteryRange = new Distance()
                    {
                        Mile = estimateBatteryRange,
                    };
                }
                if (Decimal.TryParse(values[12], out Decimal heading))
                {
                    Heading = heading;
                }
                switch (values[9])
                {
                    case "D":
                        {
                            ShiftState = Model.ShiftState.D;
                            break;
                        }
                    case "N":
                        {
                            ShiftState = Model.ShiftState.N;
                            break;
                        }
                    case "R":
                        {
                            ShiftState = Model.ShiftState.R;
                            break;
                        }
                    case "P":
                        {
                            ShiftState = Model.ShiftState.P;
                            break;
                        }
                }
            }
            catch
            {
            }
        }

        public Distance Speed { get; init; }
        public Distance Odometer { get; init; }
        public Decimal? BatteryLevel { get; init; }
        public Decimal? Elevation { get; init; }
        public Decimal? Latitude { get; init; }
        public Decimal? Longitude { get; init; }
        public Decimal? Power { get; init; }
        public ShiftState? ShiftState { get; init; }
        public Distance EstimateBatteryRange { get; init; }
        public Distance BatteryRange { get; init; }
        public Decimal? Heading { get; init; }
        public Decimal? EstimateHeading { get; init; }
        public Instant Timestamp { get; init; }
    }
}
