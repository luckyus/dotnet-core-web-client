using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Services
{
	public class WebSocketInit
	{
		public WebSocketCommand Command { get; set; }
		public string Name { get; set; }
		public string IpPort { get; set; }
	}

	public enum WebSocketCommand
	{
		Init = 0,
		AckMsg = 1,
		ChatMsg = 2,
		Error = 99
	}

}
