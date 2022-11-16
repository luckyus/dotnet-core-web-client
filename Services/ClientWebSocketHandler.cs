using dotnet_core_web_client.Models;
using System;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
		protected string regCode;
		protected bool isToReconnect = true;

		readonly string timeStampPath = Directory.GetCurrentDirectory() + "/DBase/timeStamp.json";

		public ClientWebSocketHandler(WebSocketHandler webSocketHandler, string sn, string ipPort, string regCode)
		{
			this.webSocketHandler = webSocketHandler;
			this.ipPort = ipPort;
			this.sn = sn;
			this.regCode = regCode;

			_ = Initialize();
		}

		public async Task Initialize()
		{
			var isConnected = false;
			var reconnectCount = 0;

			object[] data;
			object jsonObj;
			string jsonStr;

			for (; ; )
			{
				// establish websocket connection with .net framework server (hub) (200827)
				using (clientWebSocket = new ClientWebSocket())
				{
					clientWebSocket.Options.UseDefaultCredentials = true;

					// it doesn't seem work when working with IIS (iGuardPayroll uses IIS) (201222)
					// - it works at the client side, ie, ReceiveAsync() will throw exception, but the iGuardPayroll
					//   won't detect client offline, might need to continue to use manual ping (201222)
					clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);

					try
					{
						// queryString not required anymore after accepting websocket connection even for orphan device (210330)
						//string queryString = $"sn={sn}";
						//await clientWebSocket.ConnectAsync(new Uri("ws://" + ipPort + "/api/websocket?" + queryString), CancellationToken.None);
						await clientWebSocket.ConnectAsync(new Uri("ws://" + ipPort + "/api/websocket"), CancellationToken.None);

						// connected! acknowledge the browser (201021)
						data = new object[] { "Connected to iGuardPayroll!" };
						jsonObj = new { eventType = "onConnected", data };
						jsonStr = JsonSerializer.Serialize(jsonObj);
						await webSocketHandler.SendAsync(jsonStr);

						// get timeStamp fm file (210203)
						string str = File.ReadAllText(timeStampPath);
						var timeStampJsonElement = JsonSerializer.Deserialize<JsonElement>(str) as JsonElement?;
						var timeStamp = timeStampJsonElement?.GetProperty("timeStamp");

						// send terminal details to iGuardPayroll (201201)
						// - finally marcus agrees to send everything to me (210104)
						WebSocketMessage webSocketMessage = new()
						{
							EventType = "OnDeviceConnected",
							Data = new object[] { webSocketHandler.Terminal, webSocketHandler.TerminalSettings, webSocketHandler.Network, timeStamp }
						};

						// append regCode to the data array for iGuard540 (221011)
						if (regCode != null && regCode != "")
						{
							var dataList = webSocketMessage.Data.ToList();
							dataList.Add(regCode);
							webSocketMessage.Data = dataList.ToArray();
						}

						string jsonString = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
						_ = SendAsync(jsonString);

						isConnected = true;
						reconnectCount = 0;

						// ready to receive message (201201)
						// - supposed to be infinite loop, unless iGuardPayroll drops the connection (201218)
						await ReceiveAsync();
					}
					catch (Exception ex)
					{
						if (!isConnected && reconnectCount == 0)
						{
							data = new object[] { ex.Message };
							jsonObj = new { eventType = "onError", data };
							jsonStr = JsonSerializer.Serialize(jsonObj);
							await webSocketHandler.SendAsync(jsonStr);
							break;
						}
					}

					// eg set to false in CloseAsync() below when browser refresh (201222)
					if (!isToReconnect) break;

					// reconnect (201218)
					data = new object[] { "Reconnecting (" + ++reconnectCount + ")..." };
					jsonObj = new { eventType = "onReconnecting", data };
					jsonStr = JsonSerializer.Serialize(jsonObj);
					await webSocketHandler.SendAsync(jsonStr);

					if (clientWebSocket != null && clientWebSocket.State != WebSocketState.Closed)
					{
						_ = clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
					}
				}

				await Task.Delay(5000);
			}
		}

		public async Task CloseAsync(bool isToReconnect = false)
		{
			// no need to further reconnect because it is closed on purpose (eg browser refresh) (201219)
			this.isToReconnect = isToReconnect;
			await clientWebSocket?.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "don't know don't care", CancellationToken.None);
		}

		public async Task SendAsync(string jsonStr)
		{
			// acknowledge the browser (201201)
			var obj = new { eventType = "Tx", data = jsonStr };
			string str = JsonSerializer.Serialize(obj);
			await webSocketHandler.SendAsync(str);

			try
			{
				var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonStr));
				await clientWebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
			}
			catch (WebSocketException ex)
			{
				// acknowledge the browser (201201)
				await webSocketHandler.SendAsync(JsonSerializer.Serialize(new { eventType = "Error", data = ex.Message }));
			}
		}

		public async Task ReceiveAsync()
		{
			try
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

					_ = ms.Seek(0, SeekOrigin.Begin);

					if (result.MessageType == WebSocketMessageType.Text)
					{
						// {"eventType":"onTerminal","data":[{"inOutControl":{"inOutTrigger":{"06:00":0,"11:30":1}},"goodList":[2163965516,750006734]},"342001084"]} (201030)
						// {"eventType":"requestTerminalInfo","data":["GSD_80478134"]}
						using var reader = new StreamReader(ms, Encoding.UTF8);
						var jsonStr = reader.ReadToEnd();

						// handle the request (201124)
						var webSocketMessage = JsonSerializer.Deserialize<WebSocketMessage>(jsonStr);

						// just to acknowledge via webpage (201124)
						if (webSocketMessage.EventType == "heartBeat")
						{
							await webSocketHandler.SendAsync(JsonSerializer.Serialize(new { eventType = "heartBeat", data = jsonStr }));
						}
						else
						{
							await webSocketHandler.SendAsync(JsonSerializer.Serialize(new { eventType = "Rx", data = jsonStr }));

							_ = (webSocketMessage.EventType switch
							{
								"GetTerminalSettings" => OnGetTerminalSettings(webSocketMessage.Id),
								"GetTerminal" => OnGetTerminal(webSocketMessage.Id),
								"GetNetwork" => OnGetNetwork(webSocketMessage.Id),
								"SetTerminalSettings" => OnSetTerminalSettings(webSocketMessage.Data, webSocketMessage.Id),
								"GetLogFile" => OnGetLogFile(webSocketMessage.Data, webSocketMessage.Id),
								"Reboot" => OnReboot(webSocketMessage.Data, webSocketMessage.Id),
								"Acknowledge" => OnAcknowledge(webSocketMessage.Id),
								"SetTimeStamp" => SetTimeStamp(webSocketMessage.Data, webSocketMessage.Id),
								"OnNewUpdate" => OnNewUpdate(webSocketMessage.Id),
								"RegistrationFailed" => OniGuardPayrollRegistrationFailed(webSocketMessage.Id),
								"UnRegistered" => OnUnRegistered(webSocketMessage.Id),
								_ => OnDefaultAsync(webSocketMessage.Id),
							});
						}
					}
					else if (result.MessageType == WebSocketMessageType.Close)
					{
						await clientWebSocket.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
			}
		}

		private async Task OnUnRegistered(Guid? id)
		{
			// this will set isToReconnect to false to avoid re-connect (221018)
			await CloseAsync();
		}

		private async Task OniGuardPayrollRegistrationFailed(Guid? id)
		{
			await SendAcknowledgeAsync(id);

			// this will set isToReconnect to false to avoid re-connect (221018)
			await CloseAsync();
		}

		private async Task OnNewUpdate(Guid? id)
		{
			Random r = new();
			await Task.Delay(r.Next(5000, 7000));

			await SendAcknowledgeAsync(id);
		}

		private async Task SetTimeStamp(object[] data, Guid? id)
		{
			if (data?.Length == 0 || id == null) return;

			int timeStamp = int.Parse(data[0].ToString());

			string jsonStr = JsonSerializer.Serialize(new { timeStamp });
			File.WriteAllText(timeStampPath, jsonStr);

			await SendAcknowledgeAsync(id);
		}

		private static object OnAcknowledge(Guid? id)
		{
			return null;
		}

		private async Task OnReboot(object[] data, Guid? id)
		{
			if (id == null) return;

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "Rebooting",
				Data = Array.Empty<object>(),
				AckId = id
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
			await SendAsync(jsonStr);

			// simulate machine reboot time (201228)
			await Task.Delay(3000);

			await SendAcknowledgeAsync(id);
		}

		private async Task OnGetLogFile(object[] data, Guid? id)
		{
			// iGuardPayroll no longer specifys filename for 540 (ref marcus' signal msg) (210129)
			string filename = "http.zip";
			byte[] file = File.Exists(filename) ? File.ReadAllBytes(filename) : Array.Empty<byte>();

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnGetLogFile",
				Data = new object[] { file },
				AckId = id
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
			await SendAsync(jsonStr);
		}

		private async Task OnSetTerminalSettings(object[] data, Guid? id)
		{
			if (data?.Length == 0 || id == null) return;

			WebSocketMessage webSocketMessage;
			string jsonStr;

			string message = data[0].ToString();
			var newTerminalSettings = JsonSerializer.Deserialize<TerminalSettings>(message);

			bool isRestartRequired = false;

			if (newTerminalSettings.TerminalId != webSocketHandler.TerminalSettings.TerminalId) isRestartRequired = true;
			else if (newTerminalSettings.TimeSync.TimeZone != webSocketHandler.TerminalSettings.TimeSync.TimeZone) isRestartRequired = true;

			if (isRestartRequired)
			{
				webSocketMessage = new WebSocketMessage
				{
					EventType = "Rebooting",
					Data = Array.Empty<object>(),
					AckId = id
				};

				jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
				await SendAsync(jsonStr);
				await Task.Delay(5000);
			}

			await SendAcknowledgeAsync(id);

			webSocketHandler.TerminalSettings = newTerminalSettings;
		}

		private async Task OnGetNetwork(Guid? id)
		{
			if (id == null) return;

			Random r = new();
			await Task.Delay(r.Next(0, 200));

			Network network = webSocketHandler.Network;

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnGetNetwork",
				Data = new object[] { network },
				AckId = id
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
			await SendAsync(jsonStr);
		}

		private async Task OnGetTerminal(Guid? id)
		{
			if (id == null) return;

			Random r = new();
			await Task.Delay(r.Next(0, 200));

			Terminal terminal = webSocketHandler.Terminal;

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnGetTerminal",
				Data = new object[] { terminal },
				AckId = id
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
			await SendAsync(jsonStr);
		}

		private async Task OnGetTerminalSettings(Guid? id)
		{
			if (id == null) return;

			Random r = new();
			await Task.Delay(r.Next(0, 200));

			TerminalSettings terminalSettings = webSocketHandler.TerminalSettings;

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnGetTerminalSettings",
				Data = new object[] { terminalSettings },
				AckId = id
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
			await SendAsync(jsonStr);
		}

		/// <summary>
		/// all unhandled eventType such as SetGoodList, SetTimeStamp, OpenDoor... etc. (210113)
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private async Task OnDefaultAsync(Guid? id)
		{
			await SendAcknowledgeAsync(id);
		}

		private async Task SendAcknowledgeAsync(Guid? ackId)
		{
			if (ackId == null) return;

			try
			{
				WebSocketMessage webSocketMessage = new()
				{
					EventType = "Acknowledge",
					Data = Array.Empty<object>(),
					AckId = ackId
				};

				string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull });
				await SendAsync(jsonStr);
			}
			catch { /* don't care */ }
		}
	}
}
