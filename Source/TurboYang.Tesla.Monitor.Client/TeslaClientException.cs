using System;

namespace TurboYang.Tesla.Monitor.Client
{
    public class TeslaServiceException : Exception
    {
        public TeslaServiceException(String message)
            : this(message, null)
        {
        }

        public TeslaServiceException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
