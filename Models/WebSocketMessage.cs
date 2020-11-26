using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Models
{
	public class WebSocketMessage
	{
		[JsonPropertyName("eventType")]
		public string EventType { get; set; }
		[JsonPropertyName("data")]
		public object[] Data { get; set; }
	}
}
