using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Models
{
	public class AccessLog
	{
		[JsonPropertyName("employeeId")]
		public string EmployeeId { get; set; }
		[JsonPropertyName("logTime")]
		public DateTime LogTime { get; set; }
		[JsonPropertyName("bodyTemperature")]
		public decimal? BodyTemperature { get; set; }
		[JsonPropertyName("thumbnail")]
		public string Thumbnail { get; set; }
		[JsonPropertyName("terminalId")]
		public string TerminalID { get; set; }
		[JsonPropertyName("jobCode")]
		public int JobCode { get; set; }
		[JsonPropertyName("status")]
		public string Status { get; set; }
		[JsonPropertyName("smartCardSN")]
		public ulong SmartCardSN { get; set; }
		[JsonPropertyName("byWhat")]
		public string ByWhat { get; set; }
	}
}
