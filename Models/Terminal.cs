using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Models
{
	public class Terminal
	{
		[Key]
		[JsonPropertyName("serialNo")]
		public string SN { get; set; } = "7100-0000-0000";
		[JsonPropertyName("firmwareVersion")]
		public string FirmwareVersion { get; set; } = "7.0.0000";
		[JsonPropertyName("hasRS485")]
		public bool HasRS485 { get; set; } = true;
		[JsonPropertyName("masterServer")]
		public string MasterServer { get; set; } = "www.iguardpayroll.com";
		[JsonPropertyName("photoServer")]
		public string PhotoServer { get; set; } = "photo.iguardpayroll.com";
		[JsonPropertyName("supportedCardType")]
		public int? SupportedCardType { get; set; } = (int)SmartCardType.MifareAndOctopus;
		[JsonPropertyName("regDate")]
		[JsonConverter(typeof(DateTimeOffsetConverter))]
		public DateTimeOffset RegDate { get; set; } = DateTimeOffset.Now;
		[JsonPropertyName("environment")]
		public string Environment { get; set; } = "development";

		// debug
		public string StupidMe { get; set; }
	}
}
