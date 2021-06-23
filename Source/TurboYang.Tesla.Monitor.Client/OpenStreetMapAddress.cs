using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace TurboYang.Tesla.Monitor.Client
{
    public record OpenStreetMapAddress
    {
        // 中国
        [JsonPropertyName("country")]
        public String Country { get; init; }
        // 360008
        [JsonPropertyName("postcode")]
        public String Postcode { get; init; }
        // 福建省
        [JsonPropertyName("state")]
        public String State { get; init; }
        // 厦门市
        [JsonPropertyName("county")]
        public String County { get; init; }
        // 厦门市
        [JsonPropertyName("city")]
        public String City { get; init; }
        // 思明区
        [JsonPropertyName("district")]
        public String District { get; init; }
        // 软件园二期
        [JsonPropertyName("village")]
        public String Village { get; init; }
        // 观日路
        [JsonPropertyName("road")]
        public String Road { get; init; }
        // 24号楼
        [JsonPropertyName("building")]
        public String Building { get; init; }

        public override String ToString()
        {
            List<String> addressList = new();

            if (!String.IsNullOrWhiteSpace(Building))
            {
                addressList.Add(Building);
            }

            if (!String.IsNullOrWhiteSpace(Road))
            {
                addressList.Add(Road);
            }

            if (!String.IsNullOrWhiteSpace(Village))
            {
                addressList.Add(Village);
            }

            if (!String.IsNullOrWhiteSpace(District))
            {
                addressList.Add(District);
            }

            if (!String.IsNullOrWhiteSpace(City))
            {
                addressList.Add(City);
            }

            if (!String.IsNullOrWhiteSpace(County))
            {
                addressList.Add(County);
            }

            if (!String.IsNullOrWhiteSpace(State))
            {
                addressList.Add(State);
            }

            if (!String.IsNullOrWhiteSpace(Postcode))
            {
                addressList.Add(Postcode);
            }

            if (!String.IsNullOrWhiteSpace(Country))
            {
                addressList.Add(Country);
            }

            return String.Join(", ", addressList.Distinct());
        }
    }
}
