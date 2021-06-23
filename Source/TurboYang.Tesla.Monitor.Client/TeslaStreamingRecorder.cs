using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

using NLog;

using TurboYang.Tesla.Monitor.Model;

namespace TurboYang.Tesla.Monitor.Client
{
    public class TeslaStreamingRecorder : IDisposable
    {
        private ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
        private Task StreamingWorker { get; set; }
        private String StreamingHost { get; } = "wss://streaming.vn.teslamotors.com/streaming/";
        private String Name { get; }
        private String AccessToken { get; }
        private Int64 VehicleId { get; }
        private JsonOptions JsonOptions { get; }
        private CancellationTokenSource CancellationTokenSource { get; set; }
        private Boolean _IsRunning = false;
        public Boolean IsRunning
        {
            get
            {
                return _IsRunning;
            }
            private set
            {
                if (_IsRunning != value)
                {
                    _IsRunning = value;

                    if (_IsRunning)
                    {
                        Logger.Info($"[{Name ?? VehicleId.ToString()}] Start Car Streaming");
                    }
                    else
                    {
                        Logger.Info($"[{Name ?? VehicleId.ToString()}] Stop Car Streaming");
                    }
                }
            }
        }
        public ConcurrentQueue<TeslaStreamingData> StreamingDatas { get; } = new ConcurrentQueue<TeslaStreamingData>();

        public TeslaStreamingRecorder(String name, String accessToken, Int64 vehicleId, JsonOptions jsonOptions)
        {
            Name = name;
            AccessToken = accessToken;
            VehicleId = vehicleId;
            JsonOptions = jsonOptions;
        }

        public void Start()
        {
            IsRunning = true;

            if (StreamingWorker == null)
            {
                CancellationTokenSource = new CancellationTokenSource();
                StreamingWorker = Task.Run(async () => await Streaming(), CancellationTokenSource.Token);
            }
        }

        public void Stop()
        {
            IsRunning = false;
            CancellationTokenSource?.Cancel();
            CancellationTokenSource = null;

            while (StreamingWorker != null && !StreamingWorker.IsCompleted)
            {
            }

            StreamingWorker?.Dispose();
            StreamingWorker = null;
        }

        private async Task Streaming()
        {
            TeslaStreamingData lastStreamingData = null;

            while (IsRunning)
            {
                try
                {
                    using (ClientWebSocket webSocketClient = new())
                    {
                        await webSocketClient.ConnectAsync(new Uri(StreamingHost), CancellationToken.None);

                        await webSocketClient.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Dictionary<String, String>()
                        {
                            { "msg_type", "data:subscribe_oauth" },
                            { "token", AccessToken },
                            { "value","speed,odometer,soc,elevation,est_heading,est_lat,est_lng,power,shift_state,range,est_rage,heading" },
                            { "tag", VehicleId.ToString() },
                        }, JsonOptions.JsonSerializerOptions)), WebSocketMessageType.Text, true, CancellationToken.None);

                        String receivedMessage = null;

                        do
                        {
                            Byte[] receiveBuffer = Array.Empty<Byte>();

                            WebSocketReceiveResult response = null;

                            do
                            {
                                Byte[] buffer = new Byte[1024];

                                response = await webSocketClient.ReceiveAsync(buffer, new CancellationTokenSource(30000).Token);

                                receiveBuffer = receiveBuffer.Concat(buffer.Take(response.Count)).ToArray();
                            } while (!response.EndOfMessage);

                            if (response.MessageType == WebSocketMessageType.Close)
                            {
                                break;
                            }

                            receivedMessage = Encoding.UTF8.GetString(receiveBuffer);

                            if (!String.IsNullOrEmpty(receivedMessage))
                            {
                                TeslaStreamingMessage streamingMessage = JsonSerializer.Deserialize<TeslaStreamingMessage>(receivedMessage, JsonOptions.JsonSerializerOptions);

                                if (streamingMessage.MessageType == TeslaStreamingMessage.MessageTypes.Update)
                                {
                                    if (streamingMessage.Value.Split(",").Length == 13)
                                    {
                                        TeslaStreamingData streamingData = new(streamingMessage.Value);

                                        if (lastStreamingData != streamingData)
                                        {
                                            Logger.Info($"[{Name ?? VehicleId.ToString()}] Streaming: {streamingMessage.Value}");
                                            StreamingDatas.Enqueue(streamingData);

                                            lastStreamingData = streamingData;
                                        }
                                    }
                                    else
                                    {
                                        Logger.Error(streamingMessage.Value);

                                        break;
                                    }
                                }
                                else if (streamingMessage.MessageType == TeslaStreamingMessage.MessageTypes.Hello)
                                {
                                    continue;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        } while (!String.IsNullOrEmpty(receivedMessage));
                    }
                }
                catch
                {
                }
            };
        }

        #region Dispose

        private Boolean Disposed { get; set; }

        ~TeslaStreamingRecorder()
        {
            Dispose(false);
        }

        protected virtual void Dispose(Boolean disposing)
        {
            if (Disposed)
            {
                return;
            }

            if (disposing)
            {
                Stop();
            }

            Disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion








        //private String StreamingHost { get; } = "wss://streaming.vn.teslamotors.com/streaming/";
        //private String AccessToken { get; }
        //private Int64 VehicleId { get; }
        //private JsonOptions JsonOptions { get; }
        //private ILogger Logger { get; } = LogManager.GetCurrentClassLogger();
        //private CancellationTokenSource CancellationTokenSource { get; set; }
        //private Task StreamingLoopTask { get; set; }
        //private Stopwatch Stopwatch { get; } = new();
        //private Boolean _IsRunning = false;
        //public Boolean IsRunning 
        //{
        //    get
        //    {
        //        return _IsRunning;
        //    } 
        //    private set
        //    {
        //        if (_IsRunning != value)
        //        {
        //            _IsRunning = value;

        //            if (_IsRunning)
        //            {
        //                Logger.Info($"Start Car Streaming: {VehicleId}");
        //            }
        //            else
        //            {
        //                Logger.Info($"Stop Car Streaming: {VehicleId}");
        //            }
        //        }
        //    }
        //}

        //private Boolean _IsTryAsleep = false;
        //public Boolean IsTryAsleep 
        //{
        //    get
        //    {
        //        return _IsTryAsleep && StreamingDatas.IsEmpty;
        //    } 
        //    private set
        //    {
        //        if (_IsTryAsleep != value)
        //        {
        //            Logger.Info($"Is Try Asleep: {(value ? "true" : "false")}");

        //            _IsTryAsleep = value;
        //            if (_IsTryAsleep)
        //            {
        //                StreamingDatas.Clear();
        //            }
        //        }
        //    }
        //}
        //private Boolean _IsCanTryAsleep = true;
        //public Boolean IsCanTryAsleep
        //{
        //    get
        //    {
        //        return _IsCanTryAsleep;
        //    }
        //    set
        //    {
        //        if (_IsCanTryAsleep != value)
        //        {
        //            Logger.Info($"Is Can Try Asleep: {(value ? "true" : "false")}");

        //            _IsCanTryAsleep = value;
        //            if (!_IsCanTryAsleep)
        //            {
        //                IsTryAsleep = false;
        //            }

        //            Stopwatch.Restart();
        //        }
        //    }
        //}
        //private Boolean _IsAsleep = true;
        //public Boolean IsAsleep
        //{
        //    get
        //    {
        //        return _IsAsleep;
        //    }
        //    set
        //    {
        //        if (_IsAsleep != value)
        //        {
        //            Logger.Info($"Is Asleep: {(value ? "true" : "false")}");

        //            _IsAsleep = value;
        //            IsTryAsleep = _IsAsleep;
        //            if (_IsAsleep)
        //            {
        //                Stop();
        //            }
        //            else
        //            {
        //                Start();
        //            }

        //            Stopwatch.Restart();
        //        }
        //    }
        //}
        //public ConcurrentQueue<TeslaStreamingData> StreamingDatas { get; } = new ConcurrentQueue<TeslaStreamingData>();

        //public TeslaStreamingWorker(String accessToken, Int64 vehicleId, JsonOptions jsonOptions)
        //{
        //    AccessToken = accessToken;
        //    VehicleId = vehicleId;
        //    JsonOptions = jsonOptions;
        //}

        //public void Start()
        //{
        //    IsRunning = true;

        //    if (StreamingLoopTask == null)
        //    {
        //        CancellationTokenSource = new CancellationTokenSource();
        //        StreamingLoopTask = Task.Run(async () => await StreamingLoop(), CancellationTokenSource.Token);
        //    }
        //}

        //public void Stop()
        //{
        //    IsRunning = false;
        //    CancellationTokenSource?.Cancel();
        //    CancellationTokenSource = null;

        //    while (StreamingLoopTask != null && !StreamingLoopTask.IsCompleted)
        //    {
        //    }

        //    StreamingLoopTask?.Dispose();
        //    StreamingLoopTask = null;
        //}

        //private async Task StreamingLoop()
        //{
        //    Stopwatch.Restart();

        //    do
        //    {
        //        try
        //        {
        //            using (ClientWebSocket webSocketClient = new())
        //            {
        //                await webSocketClient.ConnectAsync(new Uri(StreamingHost), CancellationToken.None);

        //                await webSocketClient.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new Dictionary<String, String>()
        //                {
        //                    { "msg_type", "data:subscribe_oauth" },
        //                    { "token", AccessToken },
        //                    { "value","speed,odometer,soc,elevation,est_heading,est_lat,est_lng,power,shift_state,range,est_rage,heading" },
        //                    { "tag", VehicleId.ToString() },
        //                }, JsonOptions.JsonSerializerOptions)), WebSocketMessageType.Text, true, CancellationToken.None);

        //                String receivedMessage = null;
        //                TeslaStreamingData lastStreamingData = null;

        //                do
        //                {
        //                    Byte[] receiveBuffer = Array.Empty<Byte>();

        //                    WebSocketReceiveResult response = null;

        //                    do
        //                    {
        //                        Byte[] buffer = new Byte[1024];

        //                        response = await webSocketClient.ReceiveAsync(buffer, new CancellationTokenSource(30000).Token);

        //                        receiveBuffer = receiveBuffer.Concat(buffer.Take(response.Count)).ToArray();
        //                    } while (!response.EndOfMessage);

        //                    if (response.MessageType == WebSocketMessageType.Close)
        //                    {
        //                        break;
        //                    }

        //                    receivedMessage = Encoding.UTF8.GetString(receiveBuffer);

        //                    if (!String.IsNullOrEmpty(receivedMessage))
        //                    {
        //                        TeslaStreamingMessage streamingMessage = JsonSerializer.Deserialize<TeslaStreamingMessage>(receivedMessage, JsonOptions.JsonSerializerOptions);

        //                        if (streamingMessage.MessageType == TeslaStreamingMessage.MessageTypes.Update)
        //                        {
        //                            if (streamingMessage.Value.Split(",").Length == 13)
        //                            {
        //                                TeslaStreamingData streamingData = new(streamingMessage.Value);

        //                                if (streamingData.ShiftState == ShiftState.N || streamingData.ShiftState == ShiftState.D || streamingData.ShiftState == ShiftState.R || (streamingData.ShiftState == null && streamingData.Power < 0))
        //                                {
        //                                    if (lastStreamingData != streamingData)
        //                                    {
        //                                        Logger.Info(streamingMessage.Value);
        //                                        IsTryAsleep = false;
        //                                        StreamingDatas.Enqueue(streamingData);

        //                                        Stopwatch.Restart();

        //                                        lastStreamingData = streamingData;
        //                                    }
        //                                }
        //                                else
        //                                {
        //                                    break;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                Logger.Error(streamingMessage.Value);

        //                                break;
        //                            }
        //                        }
        //                        else if (streamingMessage.MessageType == TeslaStreamingMessage.MessageTypes.Hello)
        //                        {
        //                            continue;
        //                        }
        //                        else
        //                        {
        //                            break;
        //                        }
        //                    }
        //                } while (!String.IsNullOrEmpty(receivedMessage));
        //            }
        //        }
        //        finally
        //        {
        //            if (Stopwatch.Elapsed >= TimeSpan.FromMinutes(5))
        //            {
        //                if (IsCanTryAsleep)
        //                {
        //                    IsTryAsleep = true;
        //                }

        //                Stopwatch.Restart();
        //            }
        //        }
        //    } while (IsRunning);
        //}

        //#region Dispose

        //private Boolean Disposed { get; set; }

        //~TeslaStreamingWorker()
        //{
        //    Dispose(false);
        //}

        //protected virtual void Dispose(Boolean disposing)
        //{
        //    if (Disposed)
        //    {
        //        return;
        //    }

        //    if (disposing)
        //    {
        //        Stop();
        //    }

        //    Disposed = true;
        //}

        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        //#endregion
    }
}
