using System;
using System.Threading;
using System.Threading.Tasks;

namespace TurboYang.Tesla.Monitor.Client
{
    public interface IOpenStreetMapClient
    {
        public Task<OpenStreetMapAddress> ReverseLookupAsync(Decimal latitude, Decimal longitude, String language, CancellationToken cancellationToken = default);
    }
}
