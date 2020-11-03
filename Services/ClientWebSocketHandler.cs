using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Services
{
    public class ClientWebSocketHandler
    {
        protected WebSocketHandler webSocketHandler;
        protected ClientWebSocket clientWebSocket;
        protected string sn;
        protected string ipPort;

        public ClientWebSocketHandler(WebSocketHandler webSocketHandler, string sn, string ipPort)
        {
            this.webSocketHandler = webSocketHandler;
            this.sn = sn;
            this.ipPort = ipPort;
            _ = Initialize();
        }

        public async Task Initialize()
        {
            var isConnected = false;
            var reconnectCount = 0;

            for (; ; )
            {
                // establish websocket connection with .net framework server (hub) (200827)
                using (clientWebSocket = new ClientWebSocket())
                {
                    clientWebSocket.Options.UseDefaultCredentials = true;

                    try
                    {
                        string queryString = $"sn={sn}";
                        await clientWebSocket.ConnectAsync(new Uri("ws://" + ipPort + "/api/websocket?" + queryString), CancellationToken.None);

                        // acknowledge the browser (201021)
                        object[] data = { "Connected to iGuardPayroll!" };
                        var jsonObj = new { eventType = "onConnected", data };
                        var jsonStr = JsonConvert.SerializeObject(jsonObj);
                        await webSocketHandler.SendAsync(jsonStr);

                        isConnected = true;
                        reconnectCount = 0;

                        await ReceiveAsync();
                    }
                    catch (Exception ex)
                    {
                        if (isConnected)
                        {
                            object[] data = { "Reconnecting (" + ++reconnectCount + ")..." };
                            var jsonObj = new { eventType = "onReconnecting", data };
                            var jsonStr = JsonConvert.SerializeObject(jsonObj);
                            await webSocketHandler.SendAsync(jsonStr);
                            // await webSocketHandler.SendAsync(JsonConvert.SerializeObject(new { command = WebSocketCommand.AckMsg, message = "Reconnecting (" + ++reconnectCount + ")..." }));
                        }
                        else
                        {
                            object[] data = { ex.Message };
                            var jsonObj = new { eventType = "onError", data };
                            var jsonStr = JsonConvert.SerializeObject(jsonObj);
                            await webSocketHandler.SendAsync(jsonStr);
                            // await webSocketHandler.SendAsync(JsonConvert.SerializeObject(new { command = WebSocketCommand.Error, message = ex.Message }));
                        }
                    }

                    if (isConnected == true)
                    {
                        await Task.Delay(5000);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        public void Send(string jsonStr)
        {
            // "{\"event\":\"OnDeviceConnected\",\"data\":[{\"terminalId\":\"iGuard\",\"description\":\"en-Us\",\"serialNo\":\"5400-5400-0540\",\"firmwareVersion\":null,\"hasRS485\":false,\"masterServer\":\"192.168.0.230\",\"photoServer\":\"photo.iguardpayroll.com\",\"supportedCardType\":null,\"regDate\":\"2020-10-27T14:10:01.2825229+08:00\",\"environment\":null}]}"
            _ = SendAsync(jsonStr);
        }

        public async Task SendAsync(string jsonStr)
        {
            try
            {
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonStr));
                await clientWebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch (WebSocketException ex)
            {
                await webSocketHandler.SendAsync("Error: " + ex.Message);
            }
        }

        public async Task ReceiveAsync()
        {
            var receiveBuffer = new ArraySegment<Byte>(new byte[100]);

            while (clientWebSocket.State == WebSocketState.Open)
            {
                using var ms = new MemoryStream();
                WebSocketReceiveResult result;

                do
                {
                    result = await clientWebSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
                    ms.Write(receiveBuffer.Array, receiveBuffer.Offset, result.Count);
                } while (!result.EndOfMessage);

                ms.Seek(0, SeekOrigin.Begin);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    // {"Command":2,"Message":"ID: ba6807e1-a47c-4208-a944-96b2a036b861"}
                    using var reader = new StreamReader(ms, Encoding.UTF8);
                    var jsonStr = reader.ReadToEnd();
                    var jsonObj = JsonConvert.DeserializeObject<WebSocketMessage>(jsonStr);

                    if (jsonObj.EventType == "onError")
                    {
                        await webSocketHandler.SendAsync(jsonStr);
                    }
                    else
                    {
                        await webSocketHandler.SendAsync("jsonStr: " + jsonStr);
                    }
                }
            }
        }

        public async Task CloseAsync()
        {
            await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "don't know", CancellationToken.None);
        }
    }
}
