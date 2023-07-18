using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Models
{
	public class Terminals
	{
		[Key]
		public string SN { get; set; }
		public string FirmwareVersion { get; set; }
		public bool HasRS485 { get; set; }
		public string MasterServer { get; set; }
		public string PhotoServer { get; set; }
		public int? SupportedCardType { get; set; }
		public DateTimeOffset RegDate { get; set; }
		public string Environment { get; set; }

		public static explicit operator Terminals(TerminalsDto terminalDto)
		{
			Terminals terminal = new()
			{
				SN = terminalDto.SN,
				FirmwareVersion = terminalDto.FirmwareVersion,
				HasRS485 = terminalDto.HasRS485,
				MasterServer = terminalDto.MasterServer,
				PhotoServer = terminalDto.PhotoServer,
				SupportedCardType = terminalDto.SupportedCardType,
				RegDate = terminalDto.RegDate,
				Environment = terminalDto.Environment
			};

			return terminal;
		}
	}
}
