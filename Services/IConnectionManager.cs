using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Services
{
	public interface IConnectionManager
	{
		public void Initialize(WebSocket webSocket);
		public Task SendAsync(string message);
		public Task ReceiveAsync();
	}
}
