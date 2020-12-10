﻿using dotnet_core_web_client.Models;
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

						// connected! acknowledge the browser (201021)
						object[] data = { "Connected to iGuardPayroll!" };
						var jsonObj = new { eventType = "onConnected", data };
						var jsonStr = JsonSerializer.Serialize(jsonObj);
						await webSocketHandler.SendAsync(jsonStr);

						// '{"eventType":"OnDeviceConnected","data":[[{"terminalId":"iGuard","description":"en-Us","serialNo":"' + wsSerialNo.value + '","firmwareVersion":"7.0.0000","hasRS485":false,"masterServer":"192.168.0.230","photoServer":"photo.iguardpayroll.com","supportedCardType":null,"regDate":"2020-10-27T14:10:01.2825229+08:00","environment":null}], 342001083]}';

						// send terminal details to iGuardPayroll (201201)
						WebSocketMessage webSocketMessage = new WebSocketMessage
						{
							EventType = "OnDeviceConnected",
							Data = new object[2]
						};

						webSocketMessage.Data[0] = new object[] { webSocketHandler.Terminal };
						webSocketMessage.Data[1] = 342001083;

						string jsonString = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
						_ = SendAsync(jsonString);

						isConnected = true;
						reconnectCount = 0;

						// ready to receive message (201201)
						await ReceiveAsync();
					}
					catch (Exception ex)
					{
						if (!isConnected && reconnectCount == 0)
						{
							object[] data = { ex.Message };
							var jsonObj = new { eventType = "onError", data };
							var jsonStr = JsonSerializer.Serialize(jsonObj);
							await webSocketHandler.SendAsync(jsonStr);
						}

						if (true)
						{
							object[] data = { "Reconnecting (" + ++reconnectCount + ")..." };
							var jsonObj = new { eventType = "onReconnecting", data };
							var jsonStr = JsonSerializer.Serialize(jsonObj);
							await webSocketHandler.SendAsync(jsonStr);
						}

						_ = clientWebSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);

						await Task.Delay(5000);
						continue;
					}
				}
				break;
			}
		}

		public async Task CloseAsync()
		{
			if (clientWebSocket != null)
			{
				// await clientWebSocket?.CloseAsync(WebSocketCloseStatus.NormalClosure, "don't know", CancellationToken.None);
				await clientWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "don't know don't care", CancellationToken.None);
			}

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

					// just to acknowledge via webpage (201124)
					_ = webSocketHandler.SendAsync(JsonSerializer.Serialize(new { eventType = "Rx", data = jsonStr }));

					// handle the request (201124)
					var webSocketMessage = JsonSerializer.Deserialize<WebSocketMessage>(jsonStr);

					if (webSocketMessage.EventType == "getTerminalSettings")
					{
						_ = OnGetTerminalSettings(webSocketMessage.Data);
					}
					else if (webSocketMessage.EventType == "getTerminal")
					{
						_ = OnGetTerminal(webSocketMessage.Data);
					}
					else if (webSocketMessage.EventType == "getNetwork")
					{
						_ = OnGetNetwork(webSocketMessage.Data);
					}
				}
			}
		}

		private async Task OnGetNetwork(object[] data)
		{
			if (data == null || data[0] == null) return;

			string requestID = data[0].ToString();
			Network network = webSocketHandler.Network;

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnGetNetwork",
				Data = new object[2]
			};
			webSocketMessage.Data[0] = network;
			webSocketMessage.Data[1] = requestID;

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			await SendAsync(jsonStr);
		}

		private async Task OnGetTerminal(object[] data)
		{
			if (data == null || data[0] == null) return;

			string requestID = data[0].ToString();
			Terminal terminal = webSocketHandler.Terminal;

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnGetTerminal",
				Data = new object[2]
			};
			webSocketMessage.Data[0] = terminal;
			webSocketMessage.Data[1] = requestID;

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			await SendAsync(jsonStr);
		}

		private async Task OnGetTerminalSettings(object[] data)
		{
			if (data == null || data[0] == null) return;

			string requestID = data[0].ToString();
			TerminalSettings terminalSettings = webSocketHandler.TerminalSettings;

			WebSocketMessage webSocketMessage = new WebSocketMessage
			{
				EventType = "OnGetTerminalSettings",
				Data = new object[2]
			};
			webSocketMessage.Data[0] = terminalSettings;
			webSocketMessage.Data[1] = requestID;

			string jsonStr = JsonSerializer.Serialize<WebSocketMessage>(webSocketMessage, new JsonSerializerOptions { IgnoreNullValues = true });
			await SendAsync(jsonStr);
		}
	}
}
