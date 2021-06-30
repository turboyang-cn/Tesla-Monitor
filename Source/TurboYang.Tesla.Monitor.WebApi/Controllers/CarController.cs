using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NodaTime;

using TurboYang.Tesla.Monitor.Client;
using TurboYang.Tesla.Monitor.Core.Extensions;
using TurboYang.Tesla.Monitor.Database;
using TurboYang.Tesla.Monitor.Database.Entities;
using TurboYang.Tesla.Monitor.Model;
using TurboYang.Tesla.Monitor.WebApi.Services;

namespace TurboYang.Tesla.Monitor.WebApi.Controllers
{
    public class CarController : BaseController
    {
        private DatabaseContext DatabaseContext { get; }
        private ITeslaClient TeslaClient { get; }
        private ITeslaService TeslaService { get; }

        public CarController(DatabaseContext databaseContext, ITeslaClient teslaClient, ITeslaService teslaService)
        {
            DatabaseContext = databaseContext;
            TeslaClient = teslaClient;
            TeslaService = teslaService;
        }

        [HttpPost, Route("SearchCars")]
        public async Task<SearchCarsResponse> SearchCarsAsync([FromBody] SearchCarsRequest request)
        {
            try
            {
                IQueryable<CarEntity> query = DatabaseContext.Car.Include(x => x.CarSetting).AsNoTracking().ApplyFilter(request.Filters)
                                                                                            .ApplySort(request.Orders)
                                                                                            .ApplyPaging(request.PageIndex, request.PageSize, out Int32 totalCount)
                                                                                            .ApplyReorganize(request.Fields);

                List<CarEntity> carEntities = await query.ToListAsync();

                return new SearchCarsResponse()
                {
                    IsSuccess = true,
                    Cars = carEntities.Select(x => new SearchCarsResponse.Car()
                    {
                        OpenId = x.OpenId,
                        CarId = x.CarId,
                        VehicleId = x.VehicleId,
                        Type = x.Type,
                        Name = x.Name,
                        Vin = x.Vin,
                        ExteriorColor = x.ExteriorColor,
                        WheelType = x.WheelType,
                        CreateBy = x.CreateBy,
                        UpdateBy = x.UpdateBy,
                        CreateTimestamp = x.CreateTimestamp,
                        UpdateTimestamp = x.UpdateTimestamp,
                        Setting = x.CarSetting != null ? new SearchCarsResponse.Car.CarSetting()
                        {
                            IsSamplingCompression = x.CarSetting.IsSamplingCompression,
                            SamplingRate = x.CarSetting.SamplingRate,
                            TryAsleepDelay = x.CarSetting.TryAsleepDelay,
                            FullPower = x.CarSetting.FullPower,
                        } : null,
                    }).ToList(),
                };
            }
            catch
            {
                return new SearchCarsResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknow Exception",
                };
            }
        }

        [HttpPost, Route("DiscoverCars")]
        public async Task<DiscoverCarsResponse> DiscoverCarsAsync([FromBody] DiscoverCarsRequest request)
        {
            try
            {
                TokenEntity tokenEntity = await DatabaseContext.Token.FirstOrDefaultAsync(x => x.OpenId == request.TokenOpenId);

                if (tokenEntity == null)
                {
                    return new DiscoverCarsResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Token Does Not Exist"
                    };
                }

                List<TeslaCar> cars = await TeslaClient.GetCarsAsync(tokenEntity.AccessToken);

                return new DiscoverCarsResponse()
                {
                    IsSuccess = true,
                    Cars = cars.Select(x => new DiscoverCarsResponse.Car()
                    {
                        CarId = x.CarId,
                        VehicleId = x.VehicleId,
                        Name = x.DisplayName,
                        Vin = x.Vin,
                        State = x.State,
                        IsInService = x.IsInService,
                    }).ToList(),
                };
            }
            catch (TeslaServiceException exception)
            {
                return new DiscoverCarsResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = exception.Message,
                };
            }
            catch
            {
                return new DiscoverCarsResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknow Exception",
                };
            }
        }

        [HttpPost, Route("SubscribeCar")]
        public async Task<SubscribeCarResponse> SubscribeCarAsync([FromBody] SubscribeCarRequest request)
        {
            try
            {
                CarEntity carEntity = await DatabaseContext.Car.FirstOrDefaultAsync(x => x.CarId == request.CarId);

                if (carEntity != null)
                {
                    return new SubscribeCarResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Car Already Subscribed"
                    };
                }

                TokenEntity tokenEntity = await DatabaseContext.Token.FirstOrDefaultAsync(x => x.OpenId == request.TokenOpenId);

                if (tokenEntity == null)
                {
                    return new SubscribeCarResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Token Does Not Exist"
                    };
                }

                TeslaCarData carData = null;
                try
                {
                    carData = await TeslaClient.GetCarDataAsync(tokenEntity.AccessToken, request.CarId);
                }
                catch
                {
                }

                Decimal? fullPower = null;
                if (carData != null && carData.CarConfig.Type == CarType.Model3)
                {
                    fullPower = 55;
                }

                carEntity = new CarEntity()
                {
                    CarId = request.CarId,
                    VehicleId = request.VehicleId,
                    Name = carData?.DisplayName,
                    Vin = carData?.Vin,
                    ExteriorColor = carData?.CarConfig?.ExteriorColor,
                    WheelType = carData?.CarConfig?.WheelType,
                    Type = carData?.CarConfig?.Type,
                    Token = tokenEntity,
                    CarSetting = new CarSettingEntity()
                    {
                        IsSamplingCompression = request.IsSamplingCompression,
                        SamplingRate = request.SamplingRate,
                        TryAsleepDelay = request.TryAsleepDelay,
                        FullPower = fullPower,
                    },
                };

                DatabaseContext.Car.Add(carEntity);

                if (carData?.CarState?.SoftwareUpdate?.Version != null)
                {
                    DatabaseContext.Fireware.Add(new FirewareEntity()
                    {
                        Version = carData.CarState.SoftwareUpdate.Version,
                        State = carData.CarState.SoftwareUpdate.Status == SoftwareUpdateState.Unavailable ? FirewareState.Updated : FirewareState.Pending,
                        Timestamp = Instant.FromDateTimeUtc(DateTime.UtcNow),

                        Car = carEntity,
                    });
                }

                await DatabaseContext.SaveChangesAsync();

                TeslaService.StartCarRecorder(carEntity.Token.AccessToken, carEntity.Id.Value, carEntity.Name, carEntity.CarId, carEntity.VehicleId.Value, carEntity.CarSetting.SamplingRate.Value, carEntity.CarSetting.TryAsleepDelay.Value, carEntity.CarSetting.IsSamplingCompression.Value);

                return new SubscribeCarResponse()
                {
                    IsSuccess = true,
                };
            }
            catch
            {
                return new SubscribeCarResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknow Exception",
                };
            }
        }

        [HttpPost, Route("UnsubscribeCar")]
        public async Task<UnsubscribeCarResponse> UnsubscribeCarAsync([FromBody] UnsubscribeCarRequest request)
        {
            try
            {
                CarEntity carEntity = await DatabaseContext.Car.FirstOrDefaultAsync(x => x.OpenId == request.CarOpenId);

                if (carEntity == null)
                {
                    return new UnsubscribeCarResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Car Does Not Exist"
                    };
                }

                DatabaseContext.Car.Remove(carEntity);

                await DatabaseContext.SaveChangesAsync();

                TeslaService.StopCarRecorder(carEntity.CarId);

                return new UnsubscribeCarResponse()
                {
                    IsSuccess = true,
                };
            }
            catch
            {
                return new UnsubscribeCarResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknow Exception",
                };
            }
        }

        [HttpPost, Route("StartCarRecorder")]
        public async Task<StartCarRecorderResponse> StartCarRecorderAsync([FromBody] StartCarRecorderRequest request)
        {
            try
            {
                CarEntity carEntity = await DatabaseContext.Car.Include(x => x.Token)
                                                               .Include(x => x.CarSetting)
                                                               .FirstOrDefaultAsync(x => x.OpenId == request.CarOpenId);

                if (carEntity == null)
                {
                    return new StartCarRecorderResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Car Does Not Exist"
                    };
                }

                TeslaService.StartCarRecorder(carEntity.Token.AccessToken, carEntity.Id.Value, carEntity.Name, carEntity.CarId, carEntity.VehicleId.Value, carEntity.CarSetting.SamplingRate.Value, carEntity.CarSetting.TryAsleepDelay.Value, carEntity.CarSetting.IsSamplingCompression.Value);

                return new StartCarRecorderResponse()
                {
                    IsSuccess = true,
                };
            }
            catch
            {
                return new StartCarRecorderResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknow Exception",
                };
            }
        }

        [HttpPost, Route("StopCarRecorder")]
        public async Task<StopCarRecorderResponse> StopCarRecorderAsync([FromBody] StopCarRecorderRequest request)
        {
            try
            {
                CarEntity carEntity = await DatabaseContext.Car.Include(x => x.Token).FirstOrDefaultAsync(x => x.OpenId == request.CarOpenId);

                if (carEntity == null)
                {
                    return new StopCarRecorderResponse()
                    {
                        IsSuccess = false,
                        ErrorMessage = "Car Does Not Exist"
                    };
                }

                TeslaService.StopCarRecorder(carEntity.CarId);

                return new StopCarRecorderResponse()
                {
                    IsSuccess = true,
                };
            }
            catch
            {
                return new StopCarRecorderResponse()
                {
                    IsSuccess = false,
                    ErrorMessage = "Unknow Exception",
                };
            }
        }

        public class SearchCarsRequest : BaseSearchRequest
        {
        }

        public class SearchCarsResponse : BaseSearchResponse
        {
            public List<Car> Cars { get; set; }

            public class Car
            {
                [JsonPropertyName("openId")]
                public Guid? OpenId { get; init; }
                [JsonPropertyName("carId")]
                public String CarId { get; init; }
                [JsonPropertyName("vehicleId")]
                public Int64? VehicleId { get; init; }
                [JsonPropertyName("type")]
                public CarType? Type { get; init; }
                [JsonPropertyName("name")]
                public String Name { get; init; }
                [JsonPropertyName("vin")]
                public String Vin { get; init; }
                [JsonPropertyName("exteriorColor")]
                public String ExteriorColor { get; init; }
                [JsonPropertyName("wheelType")]
                public String WheelType { get; init; }
                [JsonPropertyName("createBy")]
                public String CreateBy { get; init; }
                [JsonPropertyName("updateBy")]
                public String UpdateBy { get; init; }
                [JsonPropertyName("createTimestamp")]
                public Instant? CreateTimestamp { get; init; }
                [JsonPropertyName("updateTimestamp")]
                public Instant? UpdateTimestamp { get; init; }
                [JsonPropertyName("setting")]
                public CarSetting Setting { get; init; }

                public class CarSetting
                {
                    [JsonPropertyName("samplingRate")]
                    public Int32? SamplingRate { get; init; }
                    [JsonPropertyName("isSamplingCompression")]
                    public Boolean? IsSamplingCompression { get; init; }
                    [JsonPropertyName("tryAsleepDelay")]
                    public Int32? TryAsleepDelay { get; init; }
                    [JsonPropertyName("fullPower")]
                    public Decimal? FullPower { get; init; }
                }
            }
        }

        public class DiscoverCarsRequest : BaseRequest
        {
            [JsonPropertyName("tokenOpenId")]
            public Guid TokenOpenId { get; set; }
        }

        public class DiscoverCarsResponse : BaseResponse
        {
            [JsonPropertyName("cars")]
            public List<Car> Cars { get; set; }

            public class Car
            {
                [JsonPropertyName("openId")]
                public Guid? OpenId { get; init; }
                [JsonPropertyName("carId")]
                public String CarId { get; init; }
                [JsonPropertyName("vehicleId")]
                public Int64? VehicleId { get; init; }
                [JsonPropertyName("vin")]
                public String Vin { get; init; }
                [JsonPropertyName("name")]
                public String Name { get; init; }
                [JsonPropertyName("state")]
                public CarState? State { get; init; }
                [JsonPropertyName("isInService")]
                public Boolean? IsInService { get; init; }
            }
        }

        public class SubscribeCarRequest : BaseRequest
        {
            [JsonPropertyName("tokenOpenId")]
            public Guid TokenOpenId { get; set; }
            [JsonPropertyName("carId")]
            public String CarId { get; set; }
            [JsonPropertyName("vehicleId")]
            public Int64 VehicleId { get; init; }
            [JsonPropertyName("samplingRate")]
            public Int32 SamplingRate { get; set; }
            [JsonPropertyName("isSamplingCompression")]
            public Boolean IsSamplingCompression { get; set; }
            [JsonPropertyName("tryAsleepDelay")]
            public Int32 TryAsleepDelay { get; set; }
        }

        public class SubscribeCarResponse : BaseResponse
        {
        }

        public class UnsubscribeCarRequest : BaseRequest
        {
            [JsonPropertyName("carOpenId")]
            public Guid CarOpenId { get; set; }
        }

        public class UnsubscribeCarResponse : BaseResponse
        {
        }

        public class StartCarRecorderRequest : BaseRequest
        {
            [JsonPropertyName("carOpenId")]
            public Guid CarOpenId { get; set; }
        }

        public class StartCarRecorderResponse : BaseResponse
        {
        }

        public class StopCarRecorderRequest : BaseRequest
        {
            [JsonPropertyName("carOpenId")]
            public Guid CarOpenId { get; set; }
        }

        public class StopCarRecorderResponse : BaseResponse
        {
        }
    }
}
