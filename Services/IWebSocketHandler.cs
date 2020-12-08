using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Services
{
	public interface IWebSocketHandler
	{
		public void OnConnected(WebSocket webSocket);
		public Task SendAsync(string message);
		public Task ReceiveAsync();
	}
}
