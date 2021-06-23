using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TurboYang.Tesla.Monitor.Client
{
    public interface ITeslaClient
    {
        public Task<TeslaToken> GetTokenAsync(String username, String password, String passcode, CancellationToken cancellationToken = default);
        public Task<TeslaToken> RefreshTokenAsync(String refreshToken, CancellationToken cancellationToken = default);
        public Task<List<TeslaCar>> GetCarsAsync(String accessToken, CancellationToken cancellationToken = default);
        public Task<TeslaCar> GetCarAsync(String accessToken, String carId, CancellationToken cancellationToken = default);
        public Task<TeslaCarData> GetCarDataAsync(String accessToken, String carId, CancellationToken cancellationToken = default);
    }
}
