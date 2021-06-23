using System;

namespace TurboYang.Tesla.Monitor.WebApi.Services
{
    public class LoggerService : ILoggerService
    {
        public void WriteLine(String message)
        {
            Console.WriteLine($"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffffffUTCzzz} {message}");
        }
    }
}
