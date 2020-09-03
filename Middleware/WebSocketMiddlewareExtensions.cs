using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dotnet_core_web_client.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace dotnet_core_web_client.Middleware
{
	public static class WebSocketMiddlewareExtensions
	{
		public static IApplicationBuilder UseMyConnection(this IApplicationBuilder builder)
		{
			return builder.UseMiddleware<ConnectionMiddleware>();
		}

		public static IServiceCollection AddConnectionManager(this IServiceCollection services)
		{
			services.AddScoped<ConnectionManager>();
			return services;
		}
	}
}
