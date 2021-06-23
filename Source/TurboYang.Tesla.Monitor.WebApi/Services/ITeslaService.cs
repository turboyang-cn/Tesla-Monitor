using System;

namespace TurboYang.Tesla.Monitor.WebApi.Services
{
    public interface ITeslaService
    {
        public void StartCarRecorder(String accessToken, Int32 entityId, String name, String carId, Int64 vehicleId, Int32 samplingRate, Int32 tryAsleepDelay, Boolean isSamplingCompression);
        public void StopCarRecorder(String carId);
    }
}
