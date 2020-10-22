using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet_core_web_client.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace dotnet_core_web_client.Middleware
{
	public static class WebSocketMiddlewareExtensions
	{
		public static IApplicationBuilder UseMyWebSocketHandler(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<WebSocketMiddleware>();
		}

		public static IApplicationBuilder UseMyWebSocketHandler(this IApplicationBuilder builder, PathString path, WebSocketHandler webSocketHandler)
		{
			return builder.Map(path, (_app) => _app.UseMiddleware<WebSocketMiddleware>(webSocketHandler));
		}

		public static IServiceCollection AddWebSocketHandler(this IServiceCollection services)
		{
			services.AddScoped<WebSocketHandler>();
			return services;
		}
	}
}
