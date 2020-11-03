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
		protected string sn;

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
						// eg. {"eventType":"Init","data":{"SN":"5400-5400-5400","IpPort":"192.168.0.138:50595"}}
						using var reader = new StreamReader(ms, Encoding.UTF8);
						string jsonStr = reader.ReadToEnd();
						var jsonObj = JsonConvert.DeserializeObject<dynamic>(jsonStr);

						string eventType = jsonObj.eventType;

						if (eventType == "onInit")
						{
							// array of objects (201103)
							object[] data = { "iGuardPayroll Connecting..." };
							var obj = new { eventType = "onConnecting", data };
							var str = JsonConvert.SerializeObject(obj);
							_ = SendAsync(str);

							sn = jsonObj.data[0].SN;
							string ipPort = jsonObj.data[0].IpPort;
							clientWebSocketHandler = new ClientWebSocketHandler(this, sn, ipPort);
						}
						else
						{
							clientWebSocketHandler.Send(jsonStr);
						}
					}
					else if (result.MessageType == WebSocketMessageType.Close)
					{
						if (clientWebSocketHandler != null) await clientWebSocketHandler.CloseAsync();
						await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
					}
				}
				catch (WebSocketException)
				{
					if (clientWebSocketHandler != null) await clientWebSocketHandler.CloseAsync();
				}
			}
		}
	}
}
