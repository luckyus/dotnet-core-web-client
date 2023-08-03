using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Models
{
	public class Networks
	{
		[Key]
		public string SN { get; set; }
		public string Interface { get; set; }
		public string Ip { get; set; }
		public int? Port { get; set; }
		public int? SslPort { get; set; }
		public string SubnetMask { get; set; }
		public string Gateway { get; set; }
		public string DnsStr { get; set; }
		[NotMapped]
		public List<string> Dns
		{
			get { return DnsStr.Split(',').ToList(); }
			set { DnsStr = string.Join(',', value); }
		}
		public string Ssid { get; set; }
		public string Password { get; set; }
		public bool IsWireless { get; set; }
		public bool IsDhcp { get; set; }

		public static explicit operator Networks(NetworksDto networkDto)
		{
			Networks network = new()
			{
				Interface = networkDto.Interface,
				Ip = networkDto.Ip,
				Port = networkDto.Port,
				SslPort = networkDto.SslPort,
				SubnetMask = networkDto.SubnetMask,
				Gateway = networkDto.Gateway,
				Dns = networkDto.Dns,
				Ssid = networkDto.Ssid,
				Password = networkDto.Password,
				IsWireless = networkDto.IsWireless,
				IsDhcp = networkDto.IsDhcp
			};

			return network;
		}
	}
}
