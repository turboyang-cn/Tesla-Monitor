using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using Microsoft.AspNetCore.Mvc;

using TurboYang.Tesla.Monitor.Core.Utilities;
using TurboYang.Utility.Jwt;

namespace TurboYang.Tesla.Monitor.Client
{
    public class TeslaClient : ITeslaClient
    {
        private String AuthorizeHost { get; } = "https://auth.tesla.com";
        private String ApiHost { get; } = "https://owner-api.teslamotors.com";
        private String ApiClientId { get; } = "81527cff06843c8634fdc09e8ac0abefb46ac849f38fe1e431c2ef2106796384";
        private String ApiClientSecret { get; } = "c7257eb71a564034f9419ee651c7d0e5f7aa6bfbd18bafb5c5c033b093bb2fa3";

        private JsonOptions JsonOptions { get; }

        public TeslaClient(JsonOptions jsonOptions)
        {
            JsonOptions = jsonOptions;
        }

        public async Task<TeslaToken> GetTokenAsync(String username, String password, String passcode, CancellationToken cancellationToken = default)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(username) || String.IsNullOrEmpty(password))
                {
                    throw new TeslaServiceException("No Credentials");
                }

                String codeVerifier = ComputeSha256(StringUtility.RandomString(86));
                String codeChallenge = Convert.ToBase64String(Encoding.UTF8.GetBytes(codeVerifier));

                using (HttpClientHandler handler = new()
                {
                    CookieContainer = new CookieContainer(),
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    AllowAutoRedirect = false,
                    UseCookies = true,
                })
                using (HttpClient client = new(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(30);
                    client.DefaultRequestHeaders.Add("User-Agent", "curl/2.7");
                    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                    client.DefaultRequestHeaders.Add("Accept", "*/*");
                    client.DefaultRequestHeaders.Add("Connection", "keep-alive");

                    (String Host, String Code) = await AuthorizeAsync(client, username, password, passcode, codeChallenge, cancellationToken);
                    (String AccessToken, String RefreshToken) = await AccessTokenAsync(client, Host, Code, codeVerifier, cancellationToken);
                    String accessToken = await TokenAsync(AccessToken, cancellationToken);

                    return new TeslaToken()
                    {
                        AccessToken = accessToken,
                        RefreshToken = RefreshToken,
                    };
                }

            }
            catch (HttpRequestException)
            {
                throw new TeslaServiceException("Network Error");
            }
            catch (TeslaServiceException)
            {
                throw;
            }
            catch
            {
                throw new TeslaServiceException("Unknown Error");
            }
        }

        public async Task<TeslaToken> RefreshTokenAsync(String refreshToken, CancellationToken cancellationToken = default)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(refreshToken))
                {
                    throw new TeslaServiceException("No Refresh Token");
                }

                using (HttpClientHandler handler = new()
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    AllowAutoRedirect = false,
                })
                using (HttpClient client = new(handler))
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    client.DefaultRequestHeaders.Add("User-Agent", "curl/2.7");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Connection", "keep-alive");

                    (String AccessToken, String RefreshToken) = await RefreshAccessTokenAsync(client, refreshToken, cancellationToken);
                    String accessToken = await TokenAsync(AccessToken, cancellationToken);

                    return new TeslaToken()
                    {
                        AccessToken = accessToken,
                        RefreshToken = RefreshToken,
                    };
                }
            }
            catch (HttpRequestException)
            {
                throw new TeslaServiceException("Network Error");
            }
            catch (TeslaServiceException)
            {
                throw;
            }
            catch
            {
                throw new TeslaServiceException("Unknown Error");
            }
        }

        public async Task<List<TeslaCar>> GetCarsAsync(String accessToken, CancellationToken cancellationToken = default)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(accessToken))
                {
                    throw new TeslaServiceException("No Token");
                }

                List<TeslaCar> cars = new();

                using (HttpClient client = GetHttpClient(accessToken))
                {
                    UriBuilder uriBuilder = new(ApiHost)
                    {
                        Port = -1,
                        Path = "/api/1/vehicles",
                    };

                    String getCarsRequestUri = uriBuilder.ToString();
                    HttpResponseMessage getCarsResponse = await client.GetAsync(getCarsRequestUri, cancellationToken);

                    if (getCarsResponse.StatusCode == HttpStatusCode.OK)
                    {
                        JsonElement resultElement = JsonSerializer.Deserialize<JsonElement>(await getCarsResponse.Content.ReadAsStringAsync(cancellationToken), JsonOptions.JsonSerializerOptions);

                        if (resultElement.TryGetProperty("response", out JsonElement responseElement))
                        {
                            cars = JsonSerializer.Deserialize<List<TeslaCar>>(responseElement.GetRawText(), JsonOptions.JsonSerializerOptions);
                        }
                        else
                        {
                            throw new TeslaServiceException("Unknown Error");
                        }
                    }
                    else if (getCarsResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new TeslaServiceException("Unauthorized");
                    }
                    else
                    {
                        throw new TeslaServiceException("Unknown Error");
                    }
                }

                return cars;
            }
            catch (HttpRequestException)
            {
                throw new TeslaServiceException("Network Error");
            }
            catch (TeslaServiceException)
            {
                throw;
            }
            catch
            {
                throw new TeslaServiceException("Unknown Error");
            }
        }

        public async Task<TeslaCar> GetCarAsync(String accessToken, String carId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(accessToken))
                {
                    throw new TeslaServiceException("No Token");
                }

                TeslaCar car = null;

                using (HttpClient client = GetHttpClient(accessToken))
                {
                    UriBuilder uriBuilder = new(ApiHost)
                    {
                        Port = -1,
                        Path = $"/api/1/vehicles/{carId}",
                    };

                    String getCarRequestUri = uriBuilder.ToString();
                    HttpResponseMessage getCarResponse = await client.GetAsync(getCarRequestUri, cancellationToken);

                    if (getCarResponse.StatusCode == HttpStatusCode.OK)
                    {
                        JsonElement resultElement = JsonSerializer.Deserialize<JsonElement>(await getCarResponse.Content.ReadAsStringAsync(cancellationToken), JsonOptions.JsonSerializerOptions);

                        if (resultElement.TryGetProperty("response", out JsonElement responseElement))
                        {
                            car = JsonSerializer.Deserialize<TeslaCar>(responseElement.GetRawText(), JsonOptions.JsonSerializerOptions);
                        }
                        else
                        {
                            throw new TeslaServiceException("Unknown Error");
                        }
                    }
                    else if (getCarResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new TeslaServiceException("Unauthorized");
                    }
                    else
                    {
                        throw new TeslaServiceException("Unknown Error");
                    }
                }

                return car;
            }
            catch (HttpRequestException)
            {
                throw new TeslaServiceException("Network Error");
            }
            catch (TeslaServiceException)
            {
                throw;
            }
            catch
            {
                throw new TeslaServiceException("Unknown Error");
            }
        }

        public async Task<TeslaCarData> GetCarDataAsync(String accessToken, String carId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(accessToken))
                {
                    throw new TeslaServiceException("No Token");
                }

                TeslaCarData car = null;

                using (HttpClient client = GetHttpClient(accessToken))
                {
                    UriBuilder uriBuilder = new(ApiHost)
                    {
                        Port = -1,
                        Path = $"/api/1/vehicles/{carId}/vehicle_data",
                    };

                    String getCarRequestUri = uriBuilder.ToString();
                    HttpResponseMessage getCarResponse = await client.GetAsync(getCarRequestUri, cancellationToken);

                    if (getCarResponse.StatusCode == HttpStatusCode.OK)
                    {
                        JsonElement resultElement = JsonSerializer.Deserialize<JsonElement>(await getCarResponse.Content.ReadAsStringAsync(cancellationToken), JsonOptions.JsonSerializerOptions);

                        if (resultElement.TryGetProperty("response", out JsonElement responseElement))
                        {
                            car = JsonSerializer.Deserialize<TeslaCarData>(responseElement.GetRawText(), JsonOptions.JsonSerializerOptions);
                        }
                        else
                        {
                            throw new TeslaServiceException("Unknown Error");
                        }
                    }
                    else if (getCarResponse.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        throw new TeslaServiceException("Unauthorized");
                    }
                    else
                    {
                        throw new TeslaServiceException("Unknown Error");
                    }
                }

                return car;
            }
            catch (HttpRequestException)
            {
                throw new TeslaServiceException("Network Error");
            }
            catch (TeslaServiceException)
            {
                throw;
            }
            catch
            {
                throw new TeslaServiceException("Unknown Error");
            }
        }

        private async Task<(String Host, String Code)> AuthorizeAsync(HttpClient client, String username, String password, String passcode, String codeChallenge, CancellationToken cancellationToken)
        {
            String state = StringUtility.RandomString(20);

            Dictionary<String, String> parameters = new()
            {
                { "client_id", "ownerapi" },
                { "code_challenge", codeChallenge },
                { "code_challenge_method", "S256" },
                { "redirect_uri", "https://auth.tesla.com/void/callback" },
                { "response_type", "code" },
                { "scope", "openid email offline_access" },
                { "state", state },
                { "login_hint", username },
            };

            UriBuilder uriBuilder = new(AuthorizeHost)
            {
                Port = -1,
                Path = "/oauth2/v3/authorize",
            };
            NameValueCollection queryStrings = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (KeyValuePair<String, String> parameter in parameters)
            {
                queryStrings[parameter.Key] = parameter.Value;
            }
            uriBuilder.Query = queryStrings.ToString();

            Int32 redirectCounter = 0;
            String getTokenRequestUri = uriBuilder.ToString();
            HttpResponseMessage getTokenResponse = null;

            do
            {
                getTokenResponse = await client.GetAsync(getTokenRequestUri, cancellationToken);

                if (getTokenResponse.StatusCode == HttpStatusCode.RedirectMethod)
                {
                    getTokenRequestUri = getTokenResponse.Headers.Location.ToString();
                }
            } while (getTokenResponse.StatusCode == HttpStatusCode.RedirectMethod && redirectCounter++ < 5);

            if (getTokenResponse.StatusCode == HttpStatusCode.RedirectMethod)
            {
                throw new TeslaServiceException("Server Redirected Too Many Times");
            }
            else if (getTokenResponse.StatusCode != HttpStatusCode.OK)
            {
                throw new TeslaServiceException("Unknown Error");
            }
            else
            {
                String getTokenResponseContent = await getTokenResponse.Content.ReadAsStringAsync(cancellationToken);

                Dictionary<String, String> tokenParameters = Regex.Matches(getTokenResponseContent, "type=\\\"hidden\\\" name=\\\"(?<Name>.*?)\\\" value=\\\"(?<Value>.*?)\\\"").ToDictionary(x => x.Groups["Name"].Value, x => x.Groups["Value"].Value);
                String host = $"{getTokenResponse.RequestMessage.RequestUri.Scheme}://{getTokenResponse.RequestMessage.RequestUri.Host}";

                return (host, await AuthorizeAsync(client, host, username, password, passcode, codeChallenge, state, tokenParameters, cancellationToken));
            }
        }

        private async Task<String> AuthorizeAsync(HttpClient client, String host, String username, String password, String passcode, String codeChallenge, String state, Dictionary<String, String> tokenParameters, CancellationToken cancellationToken)
        {
            Dictionary<String, String> parameters = new()
            {
                { "client_id", "ownerapi" },
                { "code_challenge", codeChallenge },
                { "code_challenge_method", "S256" },
                { "redirect_uri", "https://auth.tesla.com/void/callback" },
                { "response_type", "code" },
                { "scope", "openid email offline_access" },
                { "state", state },
            };

            UriBuilder uriBuilder = new(host)
            {
                Port = -1,
                Path = "/oauth2/v3/authorize",
            };
            NameValueCollection queryStrings = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (KeyValuePair<String, String> parameter in parameters)
            {
                queryStrings[parameter.Key] = parameter.Value;
            }
            uriBuilder.Query = queryStrings.ToString();

            String postTokenRequestUri = uriBuilder.ToString();
            HttpResponseMessage postTokenResponse = await client.PostAsync(postTokenRequestUri, new FormUrlEncodedContent(tokenParameters.Concat(new Dictionary<String, String>()
            {
                { "identity", username },
                { "credential", password },
            })), cancellationToken);

            if (postTokenResponse.StatusCode == HttpStatusCode.Redirect && postTokenResponse.Headers.Location != null)
            {
                return HttpUtility.ParseQueryString(postTokenResponse.Headers.Location.Query).Get("code");
            }
            else if (postTokenResponse.StatusCode == HttpStatusCode.OK)
            {
                if ((await postTokenResponse.Content.ReadAsStringAsync(cancellationToken)).Contains("passcode") && tokenParameters.TryGetValue("transaction_id", out String transactionId))
                {
                    return await MfaFactorAsync(client, passcode, codeChallenge, state, transactionId, cancellationToken);
                }
                else
                {
                    throw new TeslaServiceException("Unknown Error");
                }
            }
            else
            {
                throw new TeslaServiceException("Wrong Credentials");
            }
        }

        private async Task<(String AccessToken, String RefreshToken)> AccessTokenAsync(HttpClient client, String host, String code, String codeVerifier, CancellationToken cancellationToken)
        {
            UriBuilder uriBuilder = new(host)
            {
                Port = -1,
                Path = "/oauth2/v3/token",
            };

            String accessTokenRequestUri = uriBuilder.ToString();
            HttpResponseMessage accessTokenResponse = await client.PostAsync(accessTokenRequestUri, new StringContent(JsonSerializer.Serialize(new Dictionary<String, String>()
            {
                { "grant_type", "authorization_code" },
                { "client_id", "ownerapi" },
                { "code", code },
                { "code_verifier", codeVerifier },
                { "redirect_uri", "https://auth.tesla.com/void/callback" },
            }), Encoding.UTF8, "application/json"), cancellationToken);

            if (accessTokenResponse.StatusCode == HttpStatusCode.OK)
            {
                JsonElement resultElement = JsonSerializer.Deserialize<JsonElement>(await accessTokenResponse.Content.ReadAsStringAsync(cancellationToken));

                if (resultElement.ValueKind == JsonValueKind.Object)
                {
                    if (resultElement.TryGetProperty("access_token", out JsonElement accessTokenElement) && resultElement.TryGetProperty("refresh_token", out JsonElement refreshTokenElement))
                    {
                        return (accessTokenElement.GetString(), refreshTokenElement.GetString());
                    }
                }

                throw new TeslaServiceException("Unknown Error");
            }
            else
            {
                throw new TeslaServiceException("Unknown Error");
            }
        }

        private async Task<(String AccessToken, String RefreshToken)> RefreshAccessTokenAsync(HttpClient client, String refreshToken, CancellationToken cancellationToken)
        {
            JwtHandler jwtHandler = new();
            JsonElement jwtElement = jwtHandler.ExtractPayload<JsonElement>(refreshToken);
            if (jwtElement.TryGetProperty("aud", out JsonElement audienceElement))
            {
                String refreshAccessTokenRequestUri = audienceElement.GetString();
                HttpResponseMessage refreshAccessTokenResponse = await client.PostAsync(refreshAccessTokenRequestUri, new StringContent(JsonSerializer.Serialize(new Dictionary<String, String>()
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", "ownerapi" },
                    { "refresh_token", refreshToken },
                    { "scope", "openid email offline_access" },
                }), Encoding.UTF8, "application/json"), cancellationToken);

                if (refreshAccessTokenResponse.StatusCode == HttpStatusCode.OK)
                {
                    JsonElement resultElement = JsonSerializer.Deserialize<JsonElement>(await refreshAccessTokenResponse.Content.ReadAsStringAsync(cancellationToken));

                    if (resultElement.ValueKind == JsonValueKind.Object)
                    {
                        if (resultElement.TryGetProperty("access_token", out JsonElement accessTokenElement) && resultElement.TryGetProperty("refresh_token", out JsonElement refreshTokenElement))
                        {
                            return (accessTokenElement.GetString(), refreshTokenElement.GetString());
                        }
                    }

                    throw new TeslaServiceException("Unknown Error");
                }
                else
                {
                    throw new TeslaServiceException("Wrong Refresh Token");
                }
            }
            else
            {
                throw new TeslaServiceException("Wrong Refresh Token");
            }
        }

        private async Task<String> TokenAsync(String accessToken, CancellationToken cancellationToken)
        {
            using (HttpClientHandler handler = new()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            })
            using (HttpClient client = new(handler))
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Connection.Add("keep-alive");
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

                UriBuilder uriBuilder = new(ApiHost)
                {
                    Port = -1,
                    Path = "/oauth/token",
                };

                String tokenRequestUri = uriBuilder.ToString();
                HttpResponseMessage tokenResponse = await client.PostAsync(tokenRequestUri, new StringContent(JsonSerializer.Serialize(new Dictionary<String, String>()
                {
                    { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                    { "client_id", ApiClientId },
                    { "client_secret", ApiClientSecret },
                }), Encoding.UTF8, "application/json"), cancellationToken);

                if (tokenResponse.StatusCode == HttpStatusCode.OK)
                {
                    JsonElement resultElement = JsonSerializer.Deserialize<JsonElement>(await tokenResponse.Content.ReadAsStringAsync(cancellationToken));

                    if (resultElement.ValueKind == JsonValueKind.Object)
                    {
                        if (resultElement.TryGetProperty("access_token", out JsonElement accessTokenElement))
                        {
                            return accessTokenElement.GetString();
                        }
                    }

                    throw new TeslaServiceException("Unknown Error");
                }
                else
                {
                    throw new TeslaServiceException("Unknown Error");
                }
            }
        }

        private async Task<String> MfaFactorAsync(HttpClient client, String passcode, String codeChallenge, String state, String transactionId, CancellationToken cancellationToken)
        {
            Dictionary<String, String> parameters = new()
            {
                { "transaction_id", transactionId },
            };

            UriBuilder uriBuilder = new(AuthorizeHost)
            {
                Port = -1,
                Path = "/oauth2/v3/authorize/mfa/factors",
            };
            NameValueCollection queryStrings = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (KeyValuePair<String, String> parameter in parameters)
            {
                queryStrings[parameter.Key] = parameter.Value;
            }
            uriBuilder.Query = queryStrings.ToString();

            String mfaFactorRequestUri = uriBuilder.ToString();
            HttpResponseMessage mfaFactorResponse = await client.GetAsync(mfaFactorRequestUri, cancellationToken);
            
            if (mfaFactorResponse.StatusCode == HttpStatusCode.OK)
            {
                JsonElement resultElement = JsonSerializer.Deserialize<JsonElement>(await mfaFactorResponse.Content.ReadAsStringAsync(cancellationToken));

                if (resultElement.TryGetProperty("data", out JsonElement dataElement))
                {
                    if (dataElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement factorElement in dataElement.EnumerateArray())
                        {
                            if (factorElement.TryGetProperty("id", out JsonElement factorIdElement))
                            {
                                String factorId = factorIdElement.GetString();

                                String code = await MfaVerifyAsync(client, passcode, codeChallenge, state, transactionId, factorId, cancellationToken);

                                if (!String.IsNullOrWhiteSpace(code))
                                {
                                    return code;
                                }
                            }
                        }
                    }
                }

                throw new TeslaServiceException("Wrong Passcode");
            }
            else
            {
                throw new TeslaServiceException("Unknown Error");
            }
        }

        private async Task<String> MfaVerifyAsync(HttpClient client, String passcode, String codeChallenge, String state, String transactionId, String factorId, CancellationToken cancellationToken)
        {
            UriBuilder uriBuilder = new(AuthorizeHost)
            {
                Port = -1,
                Path = "/oauth2/v3/authorize/mfa/verify",
            };

            String mfaVerifyRequestUri = uriBuilder.ToString();
            HttpResponseMessage mfaVerifyResponse = await client.PostAsync(mfaVerifyRequestUri, new StringContent(JsonSerializer.Serialize(new Dictionary<String, String>()
            {
                { "factor_id", factorId },
                { "passcode", passcode },
                { "transaction_id", transactionId },
            }), Encoding.UTF8, "application/json"), cancellationToken);

            if (mfaVerifyResponse.StatusCode == HttpStatusCode.OK)
            {
                JsonElement resultElement = JsonSerializer.Deserialize<JsonElement>(await mfaVerifyResponse.Content.ReadAsStringAsync(cancellationToken));

                if (resultElement.ValueKind == JsonValueKind.Object)
                {
                    JsonElement dataElement = resultElement.GetProperty("data");

                    if (dataElement.TryGetProperty("valid", out JsonElement validElement))
                    {
                        if (validElement.ValueKind == JsonValueKind.True || validElement.ValueKind == JsonValueKind.False)
                        {
                            Boolean valid = validElement.GetBoolean();

                            if (valid)
                            {
                                return await MfaAuthorizeAsync(client, codeChallenge, state, transactionId, cancellationToken);
                            }
                        }
                    }
                }

                return null;
            }
            else
            {
                throw new TeslaServiceException("Wrong Passcode");
            }
        }

        private async Task<String> MfaAuthorizeAsync(HttpClient client, String codeChallenge, String state, String transactionId, CancellationToken cancellationToken)
        {
            Dictionary<String, String> parameters = new()
            {
                { "client_id", "ownerapi" },
                { "code_challenge", codeChallenge },
                { "code_challenge_method", "S256" },
                { "redirect_uri", "https://auth.tesla.com/void/callback" },
                { "response_type", "code" },
                { "scope", "openid email offline_access" },
                { "state", state },
            };

            UriBuilder uriBuilder = new(AuthorizeHost)
            {
                Port = -1,
                Path = "/oauth2/v3/authorize",
            };
            NameValueCollection queryStrings = HttpUtility.ParseQueryString(uriBuilder.Query);
            foreach (KeyValuePair<String, String> parameter in parameters)
            {
                queryStrings[parameter.Key] = parameter.Value;
            }
            uriBuilder.Query = queryStrings.ToString();

            String mfaAuthorizeRequestUri = uriBuilder.ToString();
            HttpResponseMessage mfaAuthorizeResponse = await client.PostAsync(mfaAuthorizeRequestUri, new FormUrlEncodedContent(new Dictionary<String, String>()
            {
                { "transaction_id", transactionId },
            }), cancellationToken);

            if (mfaAuthorizeResponse.StatusCode == HttpStatusCode.Redirect && mfaAuthorizeResponse.Headers.Location != null)
            {
                return HttpUtility.ParseQueryString(mfaAuthorizeResponse.Headers.Location.Query).Get("code");
            }
            else
            {
                throw new TeslaServiceException("Unknown Error");
            }
        }

        private HttpClient GetHttpClient(String accessToken)
        {
            HttpClient client = new();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.Timeout = TimeSpan.FromSeconds(30);

            return client;
        }

        private String ComputeSha256(String content)
        {
            StringBuilder resultBuilder = new();

            using (SHA256 sha256 = SHA256.Create())
            {
                Byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));

                for (Int32 i = 0; i < hash.Length; i++)
                {
                    resultBuilder.Append(hash[i].ToString("x2"));
                }

                return resultBuilder.ToString();
            }
        }
    }
}
