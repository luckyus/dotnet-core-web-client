using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Models
{
	public class TerminalSettings
	{
		[JsonPropertyName("terminalId")]
		public string TerminalId { get; set; }
		[JsonPropertyName("description")]
		public string Description { get; set; }
		[JsonPropertyName("language")]
		public string Language { get; set; }
		[JsonPropertyName("server")]
		public string Server { get; set; }
		[JsonPropertyName("photoServer")]
		public string PhotoServer { get; set; }
		[JsonPropertyName("dateTimeFormat")]
		public string DateTimeFormat { get; set; }
		[JsonPropertyName("allowedOrigins")]
		public string[] AllowedOrigins { get; set; }
		[JsonPropertyName("cameraControl")]
		public CameraControl CameraControl { get; set; }
		[JsonPropertyName("smartCardControl")]
		public SmartCardControl SmartCardControl { get; set; }
		[JsonPropertyName("inOutControl")]
		public InOutControl InOutControl { get; set; }
		[JsonPropertyName("remoteDoorRelayControl")]
		public RemoteDoorRelayControl RemoteDoorRelayControl { get; set; }
		[JsonPropertyName("dailyReboot")]
		public DailyReboot DailyReboot { get; set; }
		[JsonPropertyName("timeSync")]
		public TimeSync TimeSync { get; set; }
	}

	public enum CameraEnvironment
	{
		Normal, DarkOrBackLight
	}

	public enum CameraResolution
	{
		r160x120 = 1,
		r320x240 = 2,
		r640x480 = 3
	}

	public class CameraControl
	{
		[JsonPropertyName("enable")]
		public bool Enable { get; set; }
		[JsonPropertyName("resolution")]
		public CameraResolution Resolution { get; set; }
		[JsonPropertyName("frameRate")]
		public int FrameRate { get; set; }
		[JsonPropertyName("environment")]
		public CameraEnvironment Environment { get; set; }
	}

	public enum SmartCardType
	{
		MifareOnly, OctopusOnly, MifareAndOctopus
	}

	public class SmartCardControl
	{
		[JsonPropertyName("isReadCardSNOnly")]
		public bool IsReadCardSNOnly { get; set; }
		[JsonPropertyName("acceptUnknownCard")]
		public bool AcceptUnknownCard { get; set; }
		[JsonPropertyName("cardType")]
		public SmartCardType CardType { get; set; }
		[JsonPropertyName("acceptUnregisteredCard")]
		public bool AcceptUnregisteredCard { get; set; }
	}

	public enum InOutStrategy
	{
		AlwaysOut, AlwaysIn, SystemInOut, SmartInOut, AutoInOut, AlwaysF1, AlwaysF2, AlwaysF3, AlwaysF4
	}

	public enum InOutStatus
	{
		IN = 0,
		OUT = 1,
		F1 = 2,
		F2 = 3,
		F3 = 4,
		F4 = 5,
		InOutFree = 6,
		IssueCard = 23,
		FPEnroll = 39,
		SetPwd = 55,
		SuspendedCard = 7,
		Unauth_IN = 128,
		Unauth_OUT = 129,
		Unauth_F1 = 130,
		Unauth_F2 = 131,
		Unauth_F3 = 132,
		Unauth_F4 = 133,
		AutoInOutFail = 255,
		Unknown
	}

	public class InOutControl
	{
		[JsonPropertyName("inOutStrategy")]
		public InOutStrategy? DefaultInOut { get; set; }   // nullable for json's IgnoreNullValues (201106)
		[JsonPropertyName("isEnableFx")]
		public bool[] IsEnableFx { get; set; }
		[JsonPropertyName("inOutTrigger")]
		public SortedDictionary<string, InOutStatus> InOutTigger { get; set; }
		[JsonPropertyName("dailyResetAutoInOut")]
		public bool DailyResetAutoInOut { get; set; }
		[JsonPropertyName("dailyResetAutoInOutTime")]
		public string DailyResetAutoInOutTime { get; set; }
	}

	public enum AccessRight
	{
		System, RegisteredCard
	}

	public class RemoteDoorRelayControl
	{
		[JsonPropertyName("enabled")]
		public bool Enabled { get; set; }
		[JsonPropertyName("id")]
		public int Id { get; set; }
		[JsonPropertyName("delayTimer")]
		public int DelayTimer { get; set; }
		[JsonPropertyName("accessRight")]
		public AccessRight AccessRight { get; set; }
	}

	public class DailyReboot
	{
		[JsonPropertyName("enabled")]
		public bool Enabled { get; set; }
		[JsonPropertyName("time")]
		public string Time { get; set; }
	}

	public class TimeSync
	{
		[JsonPropertyName("timeZone")]
		public string TimeZone { get; set; }
		[JsonPropertyName("timeServer")]
		public string TimeServer { get; set; }
		[JsonPropertyName("isEnableSNTP")]
		public bool IsEnableSNTP { get; set; }
		[JsonPropertyName("isSyncMasterTime")]
		public bool IsSyncMasterTime { get; set; }
	}
}
