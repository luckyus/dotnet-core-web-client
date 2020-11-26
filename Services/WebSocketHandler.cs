using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dotnet_core_web_client.Models;

namespace dotnet_core_web_client.Services
{
	public class WebSocketHandler : IWebSocketHandler
	{
		protected WebSocket webSocket;
		protected string sn;

		Terminal terminal;
		TerminalSettings terminalSettings;

		public WebSocketHandler()
		{
			string folder = Directory.GetCurrentDirectory() + "/DBase";
			terminal = InitTerminal(folder + "/terminal.json");
			terminalSettings = InitTerminalSettings(folder + "/terminalSettings.json");
		}

		public void OnConnected(WebSocket webSocket)
		{
			this.webSocket = webSocket;
		}

		public Task SendAsync(string message)
		{
			var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
			return webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
		}

		public async Task ReceiveAsync()
		{
			ClientWebSocketHandler clientWebSocketHandler = null;
			var receiveBuffer = new ArraySegment<Byte>(new byte[100]);

			while (webSocket.State == WebSocketState.Open)
			{
				using var ms = new MemoryStream();
				WebSocketReceiveResult result;

				try
				{
					do
					{
						result = await webSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
						ms.Write(receiveBuffer.Array, receiveBuffer.Offset, result.Count);
					} while (!result.EndOfMessage);

					ms.Seek(0, SeekOrigin.Begin);

					if (result.MessageType == WebSocketMessageType.Text)
					{
						// eg. {"eventType":"Init","data":{"SN":"5400-5400-5400","IpPort":"192.168.0.138:50595"}}
						using var reader = new StreamReader(ms, Encoding.UTF8);
						string jsonStr = reader.ReadToEnd();
						var jsonObj = JsonSerializer.Deserialize<WebSocketMessage>(jsonStr);

						string eventType = jsonObj.EventType;

						if (eventType == "onInit")
						{
							// array of objects (201103)
							object[] data = { "iGuardPayroll Connecting..." };
							var obj = new { eventType = "onConnecting", data };
							var str = JsonSerializer.Serialize(obj);
							_ = SendAsync(str);

							var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonObj.Data[0].ToString()) as JsonElement?;
							var sn = jsonElement?.GetProperty("SN").GetString();
							var ipPort = jsonElement?.GetProperty("IpPort").GetString();
							clientWebSocketHandler = new ClientWebSocketHandler(this, sn, ipPort);
						}
						else
						{
							await clientWebSocketHandler.SendAsync(jsonStr);
						}
					}
					else if (result.MessageType == WebSocketMessageType.Close)
					{
						if (clientWebSocketHandler != null)
						{
							try { await clientWebSocketHandler.CloseAsync(); }
							catch { /* who cares? */ }
						}
						await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
					}
				}
				catch (WebSocketException)
				{
					if (clientWebSocketHandler != null) await clientWebSocketHandler.CloseAsync();
				}
			}
		}

		private Terminal InitTerminal(string path)
		{
			Terminal terminal;

			if (!File.Exists(path))
			{
				var random = new Random();

				terminal = new Terminal
				{
					TerminalId = "iGuard540",
					Description = "My iGuardExpress 540 Machine",
					SerialNo = "7100-" + random.Next(1000, 9999) + "-" + random.Next(1000, 9999),
					FirmwareVersion = "7.0.0000",
					HasRS485 = true,
					MasterServer = "www.iguardpayroll.com",
					PhotoServer = "photo.iguardpayroll.com",
					SupportedCardType = (int)SmartCardType.MifareAndOctopus,
					RegDate = DateTime.Now,
					Environment = "development",
				};

				string jsonStr = JsonSerializer.Serialize<Terminal>(terminal, new JsonSerializerOptions { IgnoreNullValues = true });
				File.WriteAllText(path, jsonStr);
			}
			else
			{
				string jsonStr = File.ReadAllText(path);
				terminal = JsonSerializer.Deserialize<Terminal>(jsonStr, new JsonSerializerOptions { IgnoreNullValues = true });
			}

			return terminal;
		}
	}
}
