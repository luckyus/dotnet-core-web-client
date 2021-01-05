using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Models
{
	public class AccessLog
	{
		[JsonPropertyName("id")]
		public string ID { get; set; }
		[JsonPropertyName("status")]
		public string Status { get; set; }
		[JsonPropertyName("logTime")]
		public DateTime LogTime { get; set; }
		[JsonPropertyName("terminalID")]
		public string TerminalID { get; set; }
		[JsonPropertyName("terminalSN")]
		public string TerminalSN { get; set; }
		[JsonPropertyName("jobCode")]
		public int JobCode { get; set; }
		[JsonPropertyName("bodyTemperature")]
		public float BodyTemperature { get; set; }
		[JsonPropertyName("dwStatus")]
		public int DwStatus { get; set; }
		[JsonPropertyName("smartCardSN")]
		public ulong SmartCardSN { get; set; }
		[JsonPropertyName("thumbnail")]
		public string Thumbnail { get; set; }
		[JsonPropertyName("photoId")]
		public Guid PhotoId { get; set; }
		[JsonPropertyName("accessPhoto")]
		public string AccessPhoto { get; set; }
		[JsonPropertyName("byWhat")]
		public string ByWhat { get; set; }
	}
}
