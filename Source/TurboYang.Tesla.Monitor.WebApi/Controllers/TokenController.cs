using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using AutoMapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NodaTime;

using TurboYang.Tesla.Monitor.Client;
using TurboYang.Tesla.Monitor.Core.Extensions;
using TurboYang.Tesla.Monitor.Database;
using TurboYang.Tesla.Monitor.Database.Entities;

namespace TurboYang.Tesla.Monitor.WebApi.Controllers
{
    public class TokenController : BaseController
    {
        private DatabaseContext DatabaseContext { get; }
        private ITeslaClient TeslaClient { get; }

        public TokenController(DatabaseContext databaseContext, ITeslaClient teslaClient, IMapper mapper)
        {
            DatabaseContext = databaseContext;
            TeslaClient = teslaClient;
        }

        [HttpPost, Route("SearchTokens")]
        public async Task<SearchTokensResponse> SearchTokensAsync([FromBody] SearchTokensRequest request)
        {
            try
            {
                IQueryable<TokenEntity> query = DatabaseContext.Token.AsNoTracking().ApplyFilter(request.Filters)
                                                                                    .ApplySort(request.Orders)
                                                                                    .ApplyPaging(request.PageIndex, request.PageSize, out Int32 totalCount)
                                                                                    .ApplyReorganize(request.Fields);

                List<TokenEntity> tokenEntities = await query.ToListAsync();

                return new SearchTokensResponse()
                {
                    IsSuccess = true,
                    Tokens = tokenEntities.Select(x => new SearchTokensResponse.Token()
                    {
                        OpenId = x.OpenId,
                        Username = x.Username,
                        CreateBy = x.CreateBy,
                        UpdateBy = x.UpdateBy,
                        CreateTimestamp = x.CreateTimestamp,
                        UpdateTimestamp = x.UpdateTimestamp,
                    }).ToList(),
                };
            }
            catch
            {
                return new SearchTokensResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknow Exception",
                };
            }
        }

        [HttpPost, Route("InsertToken")]
        public async Task<InsertTokenResponse> InsertTokenAsync([FromBody] InsertTokenRequest request)
        {
            try
            {
                TokenEntity tokenEntity = await DatabaseContext.Token.FirstOrDefaultAsync(x => x.Username == request.Username);

                if (tokenEntity != null)
                {
                    return new InsertTokenResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Token Already Exists"
                    };
                }

                TeslaToken token = await TeslaClient.GetTokenAsync(request.Username, request.Password, request.Passcode);

                tokenEntity = new TokenEntity()
                {
                    Username = request.Username,
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken,
                };

                DatabaseContext.Token.Add(tokenEntity);

                await DatabaseContext.SaveChangesAsync();

                return new InsertTokenResponse()
                {
                    IsSuccess = true,
                };
            }
            catch (TeslaServiceException exception)
            {
                return new InsertTokenResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = exception.Message,
                };
            }
            catch
            {
                return new InsertTokenResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknow Exception",
                };
            }
        }

        [HttpPost, Route("DeleteToken")]
        public async Task<DeleteTokenResponse> DeleteTokenAsync([FromBody] DeleteTokenRequest request)
        {
            try
            {
                TokenEntity tokenEntity = await DatabaseContext.Token.FirstOrDefaultAsync(x => x.OpenId == request.TokenOpenId);

                if (tokenEntity == null)
                {
                    return new DeleteTokenResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Token Does Not Exist"
                    };
                }

                DatabaseContext.Token.Remove(tokenEntity);

                await DatabaseContext.SaveChangesAsync();

                return new DeleteTokenResponse()
                {
                    IsSuccess = true,
                };
            }
            catch
            {
                return new DeleteTokenResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknow Exception",
                };
            }
        }

        [HttpPost, Route("RefreshToken")]
        public async Task<RefreshTokenResponse> RefreshTokenAsync([FromBody] RefreshTokenRequest request)
        {
            try
            {
                TokenEntity tokenEntity = await DatabaseContext.Token.FirstOrDefaultAsync(x => x.OpenId == request.TokenOpenId);

                if (tokenEntity == null)
                {
                    return new RefreshTokenResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Token Does Not Exist"
                    };
                }

                TeslaToken token = await TeslaClient.RefreshTokenAsync(tokenEntity.RefreshToken);

                tokenEntity.AccessToken = token.AccessToken;
                tokenEntity.RefreshToken = token.RefreshToken;

                await DatabaseContext.SaveChangesAsync();

                return new RefreshTokenResponse()
                {
                    IsSuccess = true,
                };
            }
            catch (TeslaServiceException exception)
            {
                return new RefreshTokenResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = exception.Message,
                };
            }
            catch
            {
                return new RefreshTokenResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknow Exception",
                };
            }
        }

        public class SearchTokensRequest : BaseSearchRequest
        {
        }

        public class SearchTokensResponse : BaseSearchResponse
        {
            public List<Token> Tokens { get; set; }

            public class Token
            {
                [JsonPropertyName("openId")]
                public Guid? OpenId { get; init; }
                [JsonPropertyName("username")]
                public String Username { get; init; }
                [JsonPropertyName("createBy")]
                public String CreateBy { get; init; }
                [JsonPropertyName("updateBy")]
                public String UpdateBy { get; init; }
                [JsonPropertyName("createTimestamp")]
                public Instant? CreateTimestamp { get; init; }
                [JsonPropertyName("updateTimestamp")]
                public Instant? UpdateTimestamp { get; init; }
            }
        }

        public class InsertTokenRequest : BaseRequest
        {
            [JsonPropertyName("username")]
            public String Username { get; set; }
            [JsonPropertyName("password")]
            public String Password { get; set; }
            [JsonPropertyName("passcode")]
            public String Passcode { get; set; }
        }

        public class InsertTokenResponse : BaseResponse
        {
        }

        public class DeleteTokenRequest : BaseRequest
        {
            [JsonPropertyName("tokenOpenId")]
            public Guid TokenOpenId { get; set; }
        }

        public class DeleteTokenResponse : BaseResponse
        {
        }

        public class RefreshTokenRequest : BaseRequest
        {
            [JsonPropertyName("tokenOpenId")]
            public Guid TokenOpenId { get; set; }
        }

        public class RefreshTokenResponse : BaseResponse
        {
        }
    }
}
