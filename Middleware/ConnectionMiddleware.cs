using dotnet_core_web_client.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Middleware
{
	public class ConnectionMiddleware
	{
		private readonly RequestDelegate next;
		private readonly ConnectionManager connectionManager;

		public ConnectionMiddleware(RequestDelegate next, ConnectionManager connectionManager)
		{
			this.next = next;
			this.connectionManager = connectionManager;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			if (context.WebSockets.IsWebSocketRequest)
			{
				WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
				connectionManager.Initialize(webSocket);
				await connectionManager.ReceiveAsync();
			}
			else
			{
				await next(context);
			}
		}
	}
}
