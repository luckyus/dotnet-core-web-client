using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using dotnet_core_web_client.DBCotexts;
using dotnet_core_web_client.Middleware;
using dotnet_core_web_client.Repository;
using dotnet_core_web_client.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace dotnet_core_web_client
{
	public class Startup
	{
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			// this is scoped service, not singleton (comment (230807)
			// - https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/
			services.AddDbContext<IGuardDBContext>(options =>
			{
				options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
			});

			services.AddMemoryCache();

			// WebSocket webSocket = null;
			// services.AddScoped<IWebSocketHandler>(x => new WebSocketHandler(webSocket));

			services.AddScoped<IWebSocketHandler, WebSocketHandler>();
			services.AddTransient<ITerminalSettingsRepository, TerminalSettingsRepository>();
			services.AddTransient<ITerminalRepository, TerminalRepository>();
			services.AddTransient<INetworkRepository, NetworkRepository>();

			services.AddControllers();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env /*, IWebSocketHandler webSocketHandler */)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseAuthorization();

			app.UseWebSockets();

			app.UseMyWebSocketHandler();

			//app.Use(async (context, next) =>
			//{
			//	if (context.WebSockets.IsWebSocketRequest)
			//	{
			//		WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
			//		// WebSocketHandler websocketHandler = new WebSocketHandler(webSocket);
			//		webSocketHandler.OnConnected(webSocket);
			//		await webSocketHandler.ReceiveAsync();
			//	}
			//	else
			//	{
			//		await next();
			//	}
			//});


			//app.Use(async (context, next) =>
			//{
			//	if (context.WebSockets.IsWebSocketRequest)
			//	{
			//		WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
			//		WebSocketHandler websocketHandler = new WebSocketHandler(webSocket);
			//		await websocketHandler.ReceiveAsync();
			//	}
			//	else
			//	{
			//		await next();
			//	}
			//});

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});

			// wwwroot/index.html (200821)
			app.UseDefaultFiles();
			app.UseStaticFiles();
		}
	}
}
