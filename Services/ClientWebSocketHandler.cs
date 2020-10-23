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
		protected WebSocketInit webSocketInit;
		protected ClientWebSocket clientWebSocket;

		public ClientWebSocketHandler(WebSocketHandler webSocketHandler, WebSocketInit webSocketInit)
		{
			this.webSocketHandler = webSocketHandler;
			this.webSocketInit = webSocketInit;
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
						string queryString = "sn=7100-1234-5678&name=" + webSocketInit.Name;
						await clientWebSocket.ConnectAsync(new Uri("wss://" + webSocketInit.IpPort + "/api/websocket?" + queryString), CancellationToken.None);

						// acknowledge the browser (201021)
						var jsonMsg = JsonConvert.SerializeObject(new { command = WebSocketCommand.AckMsg, message = "Connected to iGuardPayroll!" });
						await webSocketHandler.SendAsync(jsonMsg);

						isConnected = true;
						reconnectCount = 0;

						await ReceiveAsync();
					}
					catch (Exception ex)
					{
						if (isConnected)
						{
							await webSocketHandler.SendAsync(JsonConvert.SerializeObject(new { command = WebSocketCommand.AckMsg, message = "Reconnecting (" + ++reconnectCount + ")..." }));
						}
						else
						{
							await webSocketHandler.SendAsync(JsonConvert.SerializeObject(new { command = WebSocketCommand.Error, message = ex.Message }));
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
			_ = SendAsync(jsonStr);
		}

		public async Task SendAsync(string jsonStr)
		{
			try
			{
				var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonStr));
				await clientWebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
			}
			catch(WebSocketException ex)
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
					var jsonObj = JsonConvert.DeserializeObject<CommandType>(jsonStr);

					if (jsonObj.Command == WebSocketCommand.ChatMsg)
					{
						await webSocketHandler.SendAsync(jsonStr);
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
