using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNetCore.Mvc;

namespace TurboYang.Tesla.Monitor.Client
{
    public class OpenStreetMapClient : IOpenStreetMapClient
    {
        private String ApiHost { get; } = "https://nominatim.openstreetmap.org";

        private JsonOptions JsonOptions { get; }

        public OpenStreetMapClient(JsonOptions jsonOptions)
        {
            JsonOptions = jsonOptions;
        }

        public async Task<OpenStreetMapAddress> ReverseLookupAsync(Decimal latitude, Decimal longitude, String language, CancellationToken cancellationToken = default)
        {
            try
            {
                using (HttpClientHandler handler = new()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    AllowAutoRedirect = false,
                })
                using (HttpClient client = new(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("Accept", "*/*");
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                    client.DefaultRequestHeaders.Add("Accept-Language", language ?? "en");
                    client.DefaultRequestHeaders.Add("User-Agent", "curl/2.7");

                    return await ReverseLookupAsync(client, latitude, longitude, cancellationToken);
                }

            }
            catch
            {
                return null;
            }
        }

        private async Task<OpenStreetMapAddress> ReverseLookupAsync(HttpClient client, Decimal latitude, Decimal longitude, CancellationToken cancellationToken = default)
        {
            Dictionary<String, String> parameters = new()
            {
                { "lat", latitude.ToString() },
                { "lon", longitude.ToString() },
                { "zoom", "18" },
                { "format", "jsonv2" },
            };

            UriBuilder uriBuilder = new(ApiHost)
            {
                Port = -1,
                Path = "/reverse",
            };
            NameValueCollection queryStrings = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (KeyValuePair<String, String> parameter in parameters)
            {
                queryStrings[parameter.Key] = parameter.Value;
            }
            uriBuilder.Query = queryStrings.ToString();

            String reverseLookupUri = uriBuilder.ToString();
            HttpResponseMessage reverseLookupResponse = await client.GetAsync(reverseLookupUri, cancellationToken);

            if (reverseLookupResponse.StatusCode != HttpStatusCode.OK)
            {
                return null;
            }
            else
            {
                JsonElement resultElement = JsonSerializer.Deserialize<JsonElement>(await reverseLookupResponse.Content.ReadAsStringAsync(cancellationToken), JsonOptions.JsonSerializerOptions);

                if (resultElement.TryGetProperty("address", out JsonElement responseElement))
                {
                    return JsonSerializer.Deserialize<OpenStreetMapAddress>(responseElement.GetRawText(), JsonOptions.JsonSerializerOptions);
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
