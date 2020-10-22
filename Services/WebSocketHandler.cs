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
	public class WebSocketHandler : IWebSocketHandler
	{
		protected WebSocket webSocket;
		protected string name;

		public WebSocketHandler() { }

		//public WebSocketHandler(WebSocket webSocket)
		//{
		//	this.webSocket = webSocket;
		//}

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
						using var reader = new StreamReader(ms, Encoding.UTF8);
						string jsonStr = reader.ReadToEnd();
						var jsonObj = JsonConvert.DeserializeAnonymousType(jsonStr, new { command = WebSocketCommand.Init });

						WebSocketCommand command = jsonObj.command;

						if (command == WebSocketCommand.Init)
						{
							var obj = new { command = WebSocketCommand.AckMsg, message = "iGuardPayroll Connecting..." };
							var str = JsonConvert.SerializeObject(obj);
							_ = SendAsync(str);

							WebSocketInit webSocketInit = JsonConvert.DeserializeObject<WebSocketInit>(jsonStr);
							name = webSocketInit.Name;
							clientWebSocketHandler = new ClientWebSocketHandler(this, webSocketInit);
						}
						else if (command == WebSocketCommand.ChatMsg)
						{
							clientWebSocketHandler.Send(jsonStr);
						}
					}
					else if (result.MessageType == WebSocketMessageType.Close)
					{
						WebSocketChatMsg webSocketChatMsg = new WebSocketChatMsg
						{
							Command = WebSocketCommand.ChatMsg,
							Name = name,
							Message = "Disconnected!!!"
						};

						var jsonStr = JsonConvert.SerializeObject(webSocketChatMsg);
						clientWebSocketHandler.Send(jsonStr);

						await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
					}
				}
				catch (WebSocketException)
				{
					await clientWebSocketHandler.CloseAsync();
				}
			}
		}
	}
}
