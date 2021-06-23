
using System;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc;

namespace TurboYang.Tesla.Monitor.WebApi.Controllers
{
    [Route("Api/{Controller}")]
    public abstract class BaseController : ControllerBase
    {
        public class BaseRequest
        {
        }

        public class BaseResponse
        {
            [JsonPropertyName("isSuccess")]
            public Boolean IsSuccess { get; set; }
            [JsonPropertyName("errorMessage")]
            public String ErrorMessage { get; set; }
        }

        public class BaseSearchRequest : BaseRequest
        {
            [JsonPropertyName("fields")]
            public String Fields { get; set; }
            [JsonPropertyName("filters")]
            public String Filters { get; set; }
            [JsonPropertyName("orders")]
            public String Orders { get; set; }
            [JsonPropertyName("pageIndex")]
            public Int32 PageIndex { get; set; } = 0;
            [JsonPropertyName("pageSize")]
            public Int32 PageSize { get; set; } = 20;
        }

        public class BaseSearchResponse : BaseResponse
        {
        }
    }
}
