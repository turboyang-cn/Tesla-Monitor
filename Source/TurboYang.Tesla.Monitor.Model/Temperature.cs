using System;

namespace TurboYang.Tesla.Monitor.Model
{
    public record Temperature
    {
        public Decimal Fahrenheit { get; set; }

        public Decimal Celsius
        {
            get
            {
                return 5 * (Fahrenheit - 32) / 9;
            }
            set
            {
                Fahrenheit = 9 * value / 5 + 32;
            }
        }

        public Decimal Kelvin
        {
            get
            {
                return Celsius + 273.15m;
            }
            set
            {
                Celsius = value - 273.15m;
            }
        }

        public override String ToString()
        {
            return $"{Celsius} ℃ | {Fahrenheit} ℉ | {Kelvin} K";
        }
    }
}
