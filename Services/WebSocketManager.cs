using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace dotnet_core_web_client.Services
{
	public class WebSocketManager
	{
		protected WebSocket webSocket;

		public WebSocketManager(WebSocket _webSocket)
		{
			webSocket = _webSocket;
		}

		public struct CommandType
		{
			public WebSocketCommand Command;
		}

		public async Task DoConnection()
		{
			await ReceiveAsync();

			/*
			var url = "wss://" + ip_port + "/api/websocket";

			// connect to .net framework server via websocket (200825)
			using var serverWebSocket = new ClientWebSocket();
			serverWebSocket.Options.UseDefaultCredentials = true;

			try
			{
				await serverWebSocket.ConnectAsync(new Uri(url), CancellationToken.None);
				await ReceiveFromServerAsync(serverWebSocket, browserWebSocket);
			}
			catch(Exception ex)
			{
				Console.WriteLine("Exception: " + ex.Message);
			}
			*/
			//string connectionID = Guid.NewGuid().ToString();

			//WebSocketClient webSocketClient = new WebSocketClient
			//{
			//	webSocket = webSocket,
			//	name = connectionID
			//};

			//WebSocketClients.TryAdd(connectionID, webSocketClient);
		}

		private async Task ReceiveAsync()
		{
			await Task.Yield();

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
//						var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, };

						var jsonObj = JsonConvert.DeserializeAnonymousType(jsonStr, new { command = WebSocketCommand.Init });

						WebSocketCommand command = jsonObj.command;

						if (command == WebSocketCommand.Init)
						{
							WebSocketInit webSocketInit = JsonConvert.DeserializeObject<WebSocketInit>(jsonStr);

							var isConnected = false;
							var reconnectCount = 0;

							for(; ;)
							{
								// establish websocket connection with .net framework server (hub) (200827)
								using ClientWebSocket clientWebSocket = new ClientWebSocket();
								clientWebSocket.Options.UseDefaultCredentials = true;

								try
								{
									await clientWebSocket.ConnectAsync(new Uri("wss://" + webSocketInit.IpPort + "/api/websocket"), CancellationToken.None);

									var jsonMsg = JsonConvert.SerializeObject(new { command = WebSocketCommand.AckMsg, message = "Connected to iGuardPayroll!" });
									await SendAsync(jsonMsg);

									isConnected = true;
									reconnectCount = 0;

									await ClientReceiveAsync(clientWebSocket);
								}
								catch (Exception ex)
								{
									if(isConnected)
									{
										await SendAsync(JsonConvert.SerializeObject(new { command = WebSocketCommand.AckMsg, message = "Reconnecting (" + ++reconnectCount + ")..." }));
									}
									else
									{
										await SendAsync(JsonConvert.SerializeObject(new { command = WebSocketCommand.Error, message = ex.Message }));
									}
								}

								if(isConnected == true)
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
					else if (result.MessageType == WebSocketMessageType.Close)
					{
						await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
					}
				}
				catch (WebSocketException)
				{

				}
			}
		}

		public Task SendAsync(string message)
		{
			var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(message));
			return webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
		}

		public class Dummy
		{
			public int Command;
			public string Message;
		}

		public async Task ClientReceiveAsync(WebSocket clientWebSocket)
		{
			await Task.Yield();

			var receiveBuffer = new ArraySegment<Byte>(new byte[100]);

				while(clientWebSocket.State == WebSocketState.Open)
				{
					string message = string.Empty;

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
						var jsonObj = JsonConvert.DeserializeObject<CommandType>(jsonStr);

						if (jsonObj.Command == WebSocketCommand.ChatMsg)
						{
							await SendAsync(jsonStr);
						}
					}
			}
		}

		private async Task ReceiveFromServerAsync(WebSocket serverWebSocket, WebSocket browserWebSocket)
		{
			var receiveBuffer = new ArraySegment<Byte>(new Byte[100]);

			try
			{
				while (serverWebSocket.State == WebSocketState.Open)
				{
					string message = string.Empty;

					// using stream to read unknown-sized result (200715)
					using var ms = new MemoryStream();
					WebSocketReceiveResult result;

					do
					{
						result = await serverWebSocket.ReceiveAsync(receiveBuffer, CancellationToken.None);
						ms.Write(receiveBuffer.Array, receiveBuffer.Offset, result.Count);

					} while (!result.EndOfMessage);

					ms.Seek(0, SeekOrigin.Begin);

					if (result.MessageType == WebSocketMessageType.Text)
					{
						using (var reader = new StreamReader(ms, Encoding.UTF8)) { message = reader.ReadToEnd(); }
						await SendAsync(message);
					}
					else if (result.MessageType == WebSocketMessageType.Close)
					{
						if (result.CloseStatus == WebSocketCloseStatus.EndpointUnavailable)
						{
						}

						await serverWebSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

						// notify the browser (200825)
						await SendAsync("Server Disconnected (ID:1)!");
					}
				}
			}
			catch (WebSocketException ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.WebSocketErrorCode);
				await SendAsync("Server Disconnected (ID:2)!");
			}
		}

		//public Task BroadcastMessage(string name, string message)
		//{
		//	WebSocketItem webSocketItem = new WebSocketItem();
		//	webSocketItem.Command = WebSocketCommand.Send;
		//	webSocketItem.Name = name;
		//	webSocketItem.Message = message;

		//	string jsonMessage = JsonSerializer.Serialize<WebSocketItem>(webSocketItem);
		//	return SendToBrowserAsync(jsonMessage);
		//}

		//public async Task SendToBrowserAsync(string jsonMessage)
		//{
		//	try
		//	{
		//		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, };
		//		WebSocketItem webSocketItem = JsonSerializer.Deserialize<WebSocketItem>(jsonMessage, options);

		//		foreach (var client in WebSocketClients)
		//		{
		//			await client.Value.webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonMessage)), WebSocketMessageType.Text, true, CancellationToken.None);
		//		}
		//	}
		//	catch (Exception)
		//	{
		//	}
		//}
	}
}
