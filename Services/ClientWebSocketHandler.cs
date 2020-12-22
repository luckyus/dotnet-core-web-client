using dotnet_core_web_client.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Encodings.Web;
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
		protected bool isToReconnect = true;

		public ClientWebSocketHandler(WebSocketHandler webSocketHandler, string sn, string ipPort)
		{
			this.webSocketHandler = webSocketHandler;
			this.ipPort = ipPort;
			this.sn = sn;

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
					clientWebSocket.Options.KeepAliveInterval = TimeSpan.FromSeconds(10);

					try
					{
						string queryString = $"sn={sn}";
						await clientWebSocket.ConnectAsync(new Uri("ws://" + ipPort + "/api/websocket?" + queryString), CancellationToken.None);

						// connected! acknowledge the browser (201021)
						data = new object[] { "Connected to iGuardPayroll!" };
						jsonObj = new { eventType = "onConnected", data };
						jsonStr = JsonSerializer.Serialize(jsonObj);
						await webSocketHandler.SendAsync(jsonStr);

						// send terminal details to iGuardPayroll (201201)
						WebSocketMessage webSocketMessage = new WebSocketMessage
						{
							EventType = "OnDeviceConnected",
							Data = new object[] { new object[] { webSocketHandler.Terminal }, 342001083 }
						};

						string jsonString = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
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

		public async Task CloseAsync()
		{
			// no need to further reconnect because it is closed on purpose (eg browser refresh) (201219)
			isToReconnect = false;
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
					if (webSocketMessage.EventType == "heartBeat") _ = webSocketHandler.SendAsync(JsonSerializer.Serialize(new { eventType = "heartBeat", data = jsonStr }));
					else _ = webSocketHandler.SendAsync(JsonSerializer.Serialize(new { eventType = "Rx", data = jsonStr }));

					_ = webSocketMessage.EventType switch
					{
						"getTerminalSettings" => OnGetTerminalSettings(webSocketMessage.Data),
						"getTerminal" => OnGetTerminal(webSocketMessage.Data),
						"getNetwork" => OnGetNetwork(webSocketMessage.Data),
						"setTerminalSettings" => OnSetTerminalSettings(webSocketMessage.Data),
						_ => OnDefault(webSocketMessage.Data),
					};
				}
				else if (result.MessageType == WebSocketMessageType.Close)
				{
					await clientWebSocket.CloseAsync(WebSocketCloseStatus.Empty, "", CancellationToken.None);
				}
			}
		}

		private async Task OnSetTerminalSettings(object[] data)
		{
			if (data == null || data.Length != 2) return;

			string message = data[0].ToString();
			string requestID = data[1].ToString();

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnAcknowledge",
				Data = new object[] { requestID }
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			await SendAsync(jsonStr);

			webSocketHandler.TerminalSettings = JsonSerializer.Deserialize<TerminalSettings>(message);
		}

		private async Task OnGetNetwork(object[] data)
		{
			if (data == null || data[0] == null) return;

			Random r = new Random();
			await Task.Delay(r.Next(0, 200));

			string requestID = data[0].ToString();
			Network network = webSocketHandler.Network;

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnGetNetwork",
				Data = new object[] { network, requestID }
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			await SendAsync(jsonStr);
		}

		private async Task OnGetTerminal(object[] data)
		{
			if (data == null || data[0] == null) return;

			Random r = new Random();
			await Task.Delay(r.Next(0, 200));

			string requestID = data[0].ToString();
			Terminal terminal = webSocketHandler.Terminal;

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnGetTerminal",
				Data = new object[] { terminal, requestID }
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			await SendAsync(jsonStr);
		}

		private async Task OnGetTerminalSettings(object[] data)
		{
			if (data == null || data[0] == null) return;

			Random r = new Random();
			await Task.Delay(r.Next(0, 200));

			string requestID = data[0].ToString();
			TerminalSettings terminalSettings = webSocketHandler.TerminalSettings;

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnGetTerminalSettings",
				Data = new object[] { terminalSettings, requestID }
			};

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			await SendAsync(jsonStr);
		}

		private static async Task OnDefault(object[] data)
		{
			await Task.CompletedTask;
		}
	}
}
