using dotnet_core_web_client.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
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
						if (!isConnected && reconnectCount == 0)
						{
							object[] data = { ex.Message };
							var jsonObj = new { eventType = "onError", data };
							var jsonStr = JsonConvert.SerializeObject(jsonObj);
							await webSocketHandler.SendAsync(jsonStr);
							// await webSocketHandler.SendAsync(JsonConvert.SerializeObject(new { command = WebSocketCommand.Error, message = ex.Message }));
						}

						if (true)
						{
							object[] data = { "Reconnecting (" + ++reconnectCount + ")..." };
							var jsonObj = new { eventType = "onReconnecting", data };
							var jsonStr = JsonConvert.SerializeObject(jsonObj);
							await webSocketHandler.SendAsync(jsonStr);
							// await webSocketHandler.SendAsync(JsonConvert.SerializeObject(new { command = WebSocketCommand.AckMsg, message = "Reconnecting (" + ++reconnectCount + ")..." }));
						}
					}

					await Task.Delay(5000);
				}
			}
		}

		public async Task SendAsync(string jsonStr)
		{
			// "{\"event\":\"OnDeviceConnected\",\"data\":[{\"terminalId\":\"iGuard\",\"description\":\"en-Us\",\"serialNo\":\"5400-5400-0540\",\"firmwareVersion\":null,\"hasRS485\":false,\"masterServer\":\"192.168.0.230\",\"photoServer\":\"photo.iguardpayroll.com\",\"supportedCardType\":null,\"regDate\":\"2020-10-27T14:10:01.2825229+08:00\",\"environment\":null}]}"
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
					// {"eventType":"onTerminal","data":[{"inOutControl":{"inOutTrigger":{"06:00":0,"11:30":1}},"goodList":[2163965516,750006734]},"342001084"]} (201030)
					// {"eventType":"requestTerminalInfo","data":["GSD_80478134"]}
					using var reader = new StreamReader(ms, Encoding.UTF8);
					var jsonStr = reader.ReadToEnd();

					// just to acknowledge via webpage (201124)
					await webSocketHandler.SendAsync(jsonStr);

					// handle the request (201124)
					var webSocketMessage = JsonConvert.DeserializeObject<WebSocketMessage>(jsonStr);

					if (webSocketMessage.EventType == "requestTerminalSettings")
					{
						await OnRequestTerminalSettings(webSocketMessage.Data);
					}
					else if(webSocketMessage.EventType == "requestTerminal")
					{
						await OnRequestTerminal(webSocketMessage.Data);
					}
				}
			}
		}

		private Task OnRequestTerminal(object[] data)
		{
			if (data == null || data[0] == null) return Task.CompletedTask;

			string requestID = (string)data[0];

			Terminal terminal = new Terminal
			{
				TerminalId = "iGuard540",
				Description = "iGuardExpress 540 Machine",
			};

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnRequestTerminal",
				Data = new object[2]
			};
			webSocketMessage.Data[0] = terminal;
			webSocketMessage.Data[1] = requestID;

			string jsonStr = System.Text.Json.JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			return SendAsync(jsonStr);
		}

		private Task OnRequestTerminalSettings(object[] data)
		{
			if (data == null || data[0] == null) return Task.CompletedTask;

			string requestID = (string)data[0];

			TerminalSettings terminalSettings = new TerminalSettings
			{
				TerminalId = "My540",
				Description = "This is my 540 Machine",
				Language = "en-US",
				Server = "www.iguardpayroll.com",
				PhotoServer = "photo.iguardpayroll.com",
				DateTimeFormat = "dd/mm/yy",
				AllowedOrigins = new string[] { "one", "two" },
				CameraControl = new CameraControl
				{
					Enable = true,
					Resolution = CameraResolution.r640x480,
					FrameRate = 1,
					Environment = CameraEnvironment.Normal
				},
				SmartCardControl = new SmartCardControl
				{
					IsReadCardSNOnly = false,
					AcceptUnknownCard = false,
					CardType = SmartCardType.OctopusOnly,
					AcceptUnregisteredCard = false
				},
				InOutControl = new InOutControl
				{
					DefaultInOut = InOutStrategy.SystemInOut,
					IsEnableFx = new bool[] { true, false, true, false },
					InOutTigger = new SortedDictionary<string, InOutStatus>()
					{
						["7:00"] = InOutStatus.IN,
						["11:30"] = InOutStatus.OUT,
						["12:30"] = InOutStatus.IN,
						["16:30"] = InOutStatus.OUT
					},
				},
				RemoteDoorRelayControl = new RemoteDoorRelayControl
				{
					Enabled = true,
					Id = 123,
					DelayTimer = 3000,
					AccessRight = AccessRight.System
				},
				DailyReboot = new DailyReboot
				{
					Enabled = true,
					Time = "02:00"
				},
				TimeSync = new TimeSync
				{
					TimeZone = "HK",
					TimeServer = "time.google.com",
					IsEnableSNTP = true,
					IsSyncMasterTime = true
				}
			};

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnRequestTerminalSettings",
				Data = new object[2]
			};
			webSocketMessage.Data[0] = terminalSettings;
			webSocketMessage.Data[1] = requestID;

			string jsonStr = System.Text.Json.JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			return SendAsync(jsonStr);
		}

		public async Task CloseAsync()
		{
			await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "don't know", CancellationToken.None);
		}
	}
}
