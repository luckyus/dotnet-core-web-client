using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using Microsoft.AspNetCore.Http;

namespace dotnet_core_web_client.Services
{
	public class WebSocketHandler : IWebSocketHandler
	{
		protected WebSocket webSocket;
		protected string iGuardPayrollIpPort;
		readonly string terminalConfigPath = Directory.GetCurrentDirectory() + "/DBase" + "/terminal.config";
		readonly string terminalSettingsConfigPath = Directory.GetCurrentDirectory() + "/DBase" + "/terminalSettings.config";
		readonly string networkConfigPath = Directory.GetCurrentDirectory() + "/DBase" + "/network.config";

		public WebSocketHandler() 
		{
		}

		private TerminalSettings _TerminalSettings = null;
		public TerminalSettings TerminalSettings
		{
			get
			{
				if (_TerminalSettings == null)
				{
					if (File.Exists(terminalSettingsConfigPath))
					{
						string jsonStr = File.ReadAllText(terminalSettingsConfigPath);
						_TerminalSettings = JsonSerializer.Deserialize<TerminalSettings>(jsonStr);
					}
					else
					{
						_TerminalSettings = new TerminalSettings();
						string jsonStr = JsonSerializer.Serialize<TerminalSettings>(_TerminalSettings, new JsonSerializerOptions { IgnoreNullValues = true });
						File.WriteAllText(terminalConfigPath, jsonStr);
					}

				}
				return _TerminalSettings;
			}
			set
			{
				string jsonStr = JsonSerializer.Serialize<TerminalSettings>(value, new JsonSerializerOptions { IgnoreNullValues = true });
				File.WriteAllText(terminalSettingsConfigPath, jsonStr);
				_TerminalSettings = value;
			}
		}

		private Terminal _Terminal = null;
		public Terminal Terminal
		{
			get
			{
				if (_Terminal == null)
				{
					if (File.Exists(terminalConfigPath))
					{
						string jsonStr = File.ReadAllText(terminalConfigPath);
						_Terminal = JsonSerializer.Deserialize<Terminal>(jsonStr);
					}
					else
					{
						_Terminal = new Terminal();
						string jsonStr = JsonSerializer.Serialize<Terminal>(_Terminal, new JsonSerializerOptions { IgnoreNullValues = true });
						File.WriteAllText(terminalConfigPath, jsonStr);
					}
				}
				return _Terminal;
			}
			set
			{
				string jsonStr = JsonSerializer.Serialize<Terminal>(value, new JsonSerializerOptions { IgnoreNullValues = true });
				File.WriteAllText(terminalConfigPath, jsonStr);
				_Terminal = value;
			}
		}

		private Network _Network = null;
		public Network Network
		{
			get
			{
				if (_Network == null)
				{
					if (File.Exists(networkConfigPath))
					{
						string jsonStr = File.ReadAllText(networkConfigPath);
						_Network = JsonSerializer.Deserialize<Network>(jsonStr);
					}
					else
					{
						_Network = new Network();
						string jsonStr = JsonSerializer.Serialize<Network>(_Network, new JsonSerializerOptions { IgnoreNullValues = true });
						File.WriteAllText(networkConfigPath, jsonStr);
					}
				}
				return _Network;
			}
			set
			{
				string jsonStr = JsonSerializer.Serialize<Network>(value, new JsonSerializerOptions { IgnoreNullValues = true });
				File.WriteAllText(networkConfigPath, jsonStr);
				_Network = value;
			}
		}

		public void OnConnected(WebSocket webSocket)
		{
			this.webSocket = webSocket;
		}

		private void SaveTerminal()
		{
			string jsonStr = JsonSerializer.Serialize<Terminal>(Terminal, new JsonSerializerOptions { IgnoreNullValues = true });
			try
			{
				File.WriteAllText(terminalConfigPath, jsonStr);
			}
			catch (Exception ex)
			{
				_ = ex.Message;
			}
			
		}

		public async Task SendAsync(string message)
		{
			if (webSocket.State == WebSocketState.Open)
			{
				var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
				await webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
			}
			return;
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

							// update the existing terminal info if necessary (201201)
							var jsonElement = JsonSerializer.Deserialize<JsonElement>(jsonObj.Data[0].ToString()) as JsonElement?;
							var sn = jsonElement?.GetProperty("SN").GetString();
							var ipPort = jsonElement?.GetProperty("IpPort").GetString();

							if (Terminal.SN != sn)
							{
								Terminal.SN = sn;
								SaveTerminal();
							}

							// connect to iGuardPayroll (201201)
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

			return;
		}
	}
}
