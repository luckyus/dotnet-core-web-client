﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dotnet_core_web_client.Models
{
	public record NetworksDto
	{
		[JsonPropertyName("interface")]
		public string Interface { get; set; } = "eth0";
		[JsonPropertyName("ip")]
		public string Ip { get; set; } = "192.168.0.117";
		[JsonPropertyName("port")]
		public int? Port { get; set; } = 80;
		[JsonPropertyName("sslPort")]
		public int? SslPort { get; set; } = 433;
		[JsonPropertyName("subnet")]
		public string SubnetMask { get; set; } = "255.255.255.0";
		[JsonPropertyName("gateway")]
		public string Gateway { get; set; } = "192.168.0.11";
		[JsonPropertyName("dns")]
		public List<string> Dns { get; set; } = new List<string> { "8.8.8.8", "8.8.4.4" };
		[JsonPropertyName("ssid")]
		public string Ssid { get; set; } = "LuckyTech";
		[JsonPropertyName("password")]
		public string Password { get; set; } = "HELLO, WORLD!";
		[JsonPropertyName("isWireless")]
		public bool IsWireless { get; set; } = true;
		[JsonPropertyName("isDHCP")]
		public bool IsDhcp { get; set; } = false;

		public static explicit operator NetworksDto(Networks network)
		{
			if (network == null)
			{
				return null;
			}

			NetworksDto dto = new()
			{
				Interface = network.Interface,
				Ip = network.Ip,
				Port = network.Port,
				SslPort = network.SslPort,
				SubnetMask = network.SubnetMask,
				Gateway = network.Gateway,
				Dns = network.Dns,
				Ssid = network.Ssid,
				Password = network.Password,
				IsWireless = network.IsWireless,
				IsDhcp = network.IsDhcp
			};

			return dto;
		}
	}
}
