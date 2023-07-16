using dotnet_core_web_client.Repository;
using dotnet_core_web_client.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Middleware
{
	public class WebSocketMiddleware
	{
		private readonly RequestDelegate next;

		// private readonly IWebSocketHandler webSocketHandler;

		// only singleton DI (services.AddSingleton<IWebSocketHandler, WebSocketHandler>()) can be injected in the constructor,
		// scoped ones can only be injected in the InvokeAsync() (200921)
		// - ref: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write?view=aspnetcore-3.1 (200921)
		public WebSocketMiddleware(RequestDelegate next /*, IWebSocketHandler _webSocketHandler */)
		{
			this.next = next;
			// this.webSocketHandler = _webSocketHandler;
		}

		public async Task InvokeAsync(HttpContext context, IServiceScopeFactory scopeFactory /*, IWebSocketHandler webSocketHandler */)
		{
			if (context.WebSockets.IsWebSocketRequest)
			{
				using var scope = scopeFactory.CreateScope();
				IWebSocketHandler webSocketHandler = scope.ServiceProvider.GetRequiredService<IWebSocketHandler>();

				WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
				webSocketHandler.OnConnected(webSocket);
				await webSocketHandler.ReceiveAsync();
			}
			else
			{
				await next(context);
			}
		}
	}
}
