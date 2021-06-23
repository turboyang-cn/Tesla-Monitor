//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//using Microsoft.AspNetCore.Mvc;

//using TurboYang.Tesla.Monitor.Core.Services;
//using TurboYang.Tesla.Monitor.Core.Utilities;

//namespace TurboYang.Tesla.Monitor.WebApi.Controllers
//{
//    [ApiController]
//    [Route("[controller]/[Action]")]
//    public class TestController : ControllerBase
//    {
//        private ITeslaTokenService TeslaTokenService { get; }
//        private ITeslaCarService TeslaCarService { get; }
//        private ILoggerService LoggerService { get; }

//        public TestController(ITeslaTokenService teslaTokenService, ITeslaCarService teslaCarService, ILoggerService loggerService)
//        {
//            TeslaTokenService = teslaTokenService;
//            TeslaCarService = teslaCarService;
//            LoggerService = loggerService;
//        }

//        [HttpGet]
//        public async Task Login()
//        {
//            //TeslaToken mfaAccountToken = await TeslaTokenService.GetTokenAsync("hk.turboyang@gmail.com", "1qazXSW@3edcVFR$", "093778");
//            //await Test(mfaAccountToken);

//            //TeslaToken usAccountToken = await TeslaTokenService.GetTokenAsync("us.turboyang@gmail.com", "1qazXSW@3edcVFR$", null);
//            //await Test(usAccountToken);

//            await Test("89900101@qq.com", "1qazXSW@3edcVFR$", null);
//        }

//        [HttpGet]
//        public async Task Login1()
//        {
//            //TeslaToken mfaAccountToken = await TeslaTokenService.GetTokenAsync("hk.turboyang@gmail.com", "1qazXSW@3edcVFR$", "093778");
//            //await Test(mfaAccountToken);

//            //TeslaToken usAccountToken = await TeslaTokenService.GetTokenAsync("us.turboyang@gmail.com", "1qazXSW@3edcVFR$", null);
//            //await Test(usAccountToken);

//            await Test1("89900101@qq.com", "1qazXSW@3edcVFR$", null);
//        }

//        private async Task Test(String username, String password, String passcode)
//        {
//            try
//            {
//                LoggerService.WriteLine($"GetToken => {StringUtility.ObfuscateString(username)}");
//                TeslaToken token = await TeslaTokenService.GetTokenAsync(username, password, passcode);

//                LoggerService.WriteLine($"GetCars => {StringUtility.ObfuscateString(username)}");
//                List<TeslaCar> cars = await TeslaCarService.GetCarsAsync(token.AccessToken);

//                foreach (TeslaCar car in cars)
//                {
//                    LoggerService.WriteLine($"StartCarLoop => {car.DisplayName}");
//                    TeslaCarService.StartCarLoop(token.AccessToken, car.CarId, car.VehicleId);
//                }
//            }
//            catch (TeslaServiceException exception)
//            {
//                LoggerService.WriteLine($"<Error> {exception.Message}");
//            }
//            catch
//            {
//                LoggerService.WriteLine("<Error> Unknow Error");
//            }
//        }

//        private async Task Test1(String username, String password, String passcode)
//        {
//            try
//            {
//                TeslaToken token = await TeslaTokenService.GetTokenAsync(username, password, passcode);

//                List<TeslaCar> cars = await TeslaCarService.GetCarsAsync(token.AccessToken);

//                foreach (TeslaCar car in cars)
//                {
//                    TeslaCarService.StopCarLoop(car.CarId);
//                    LoggerService.WriteLine($"StopCarLoop => {car.DisplayName}");
//                }
//            }
//            catch (TeslaServiceException exception)
//            {
//                LoggerService.WriteLine($"<Error> {exception.Message}");
//            }
//            catch
//            {
//                LoggerService.WriteLine("<Error> Unknow Error");
//            }
//        }
//    }
//}
