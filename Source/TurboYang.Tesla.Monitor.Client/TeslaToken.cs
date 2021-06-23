using System;

namespace TurboYang.Tesla.Monitor.Client
{
    public record TeslaToken
    {
        public String AccessToken { get; init; }
        public String RefreshToken { get; init; }
    }
}
