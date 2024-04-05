using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System;
using dotnet_core_web_client.Utilities;

namespace dotnet_core_web_client.Models
{
    public record TerminalsDto
	{
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

		public static explicit operator TerminalsDto(Terminals terminal)
		{
			if (terminal == null)
			{
				return null;
			}

			TerminalsDto dto = new()
			{
				SN = terminal.SN,
				FirmwareVersion = terminal.FirmwareVersion,
				HasRS485 = terminal.HasRS485,
				MasterServer = terminal.MasterServer,
				PhotoServer = terminal.PhotoServer,
				SupportedCardType = terminal.SupportedCardType,
				RegDate = terminal.RegDate,
				Environment = terminal.Environment
			};

			return dto;
		}
	}
}
