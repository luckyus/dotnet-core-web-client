using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Models
{
	public class Terminal
	{
		[JsonPropertyName("terminalId")]
		public string TerminalId { get; set; }
		[JsonPropertyName("description")]
		public string Description { get; set; }
		[JsonPropertyName("serialNo")]
		public string SerialNo { get; set; }
		[JsonPropertyName("firmwareVersion")]
		public string FirmwareVersion { get; set; }
		[JsonPropertyName("hasRS485")]
		public bool HasRS485 { get; set; }
		[JsonPropertyName("masterServer")]
		public string MasterServer { get; set; }
		[JsonPropertyName("photoServer")]
		public string PhotoServer { get; set; }
		[JsonPropertyName("supportedCardType")]
		public int? SupportedCardType { get; set; }
		[JsonPropertyName("regDate")]
		public DateTime RegDate { get; set; }
		[JsonPropertyName("environment")]
		public string Environment { get; set; }
	}
}
