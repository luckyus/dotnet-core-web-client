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
		public string TerminalId { get; set; } = "iGuard540";
		[JsonPropertyName("description")]
		public string Description { get; set; } = "My iGuardExpress 540 Machine";
		[JsonPropertyName("language")]
		public string Language { get; set; } = "en-us";
		//[JsonPropertyName("server")]
		//public string Server { get; set; } = "www.iguardpayroll.com";
		//[JsonPropertyName("photoServer")]
		//public string PhotoServer { get; set; } = "photo.iguardpayroll.com";
		[JsonPropertyName("dateTimeFormat")]
		public string DateTimeFormat { get; set; } = "dd/mm/yy";
		[JsonPropertyName("allowedOrigins")]
		public string[] AllowedOrigins { get; set; } = new string[] { "one", "two" };
		[JsonPropertyName("cameraControl")]
		public CameraControl CameraControl { get; set; } = new CameraControl();
		[JsonPropertyName("smartCardControl")]
		public SmartCardControl SmartCardControl { get; set; } = new SmartCardControl();
		[JsonPropertyName("inOutControl")]
		public InOutControl InOutControl { get; set; } = new InOutControl();
		[JsonPropertyName("inOutTrigger")]
		public SortedDictionary<string, InOutStatus> InOutTigger { get; set; } = new SortedDictionary<string, InOutStatus>()
		{
			["7:00"] = InOutStatus.IN,
			["11:30"] = InOutStatus.OUT,
			["12:30"] = InOutStatus.IN,
			["16:30"] = InOutStatus.OUT
		};
		[JsonPropertyName("remoteDoorRelayControl")]
		public RemoteDoorRelayControl RemoteDoorRelayControl { get; set; } = new RemoteDoorRelayControl();
		[JsonPropertyName("dailyReboot")]
		public DailyReboot DailyReboot { get; set; } = new DailyReboot();
		[JsonPropertyName("timeSync")]
		public TimeSync TimeSync { get; set; } = new TimeSync();
		[JsonPropertyName("tempDetectEnable")]
		public bool TempDetectEnable { get; set; }
		[JsonPropertyName("faceDetectEnable")]
		public bool FaceDetectEnable { get; set; }
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
		public bool Enable { get; set; } = true;
		[JsonPropertyName("resolution")]
		public CameraResolution Resolution { get; set; } = CameraResolution.r640x480;
		[JsonPropertyName("frameRate")]
		public int FrameRate { get; set; } = 1;
		[JsonPropertyName("environment")]
		public CameraEnvironment Environment { get; set; } = CameraEnvironment.Normal;
	}

	public enum SmartCardType
	{
		MifareOnly, OctopusOnly, MifareAndOctopus
	}

	public class SmartCardControl
	{
		[JsonPropertyName("isReadCardSNOnly")]
		public bool IsReadCardSNOnly { get; set; } = false;
		[JsonPropertyName("acceptUnknownCard")]
		public bool AcceptUnknownCard { get; set; } = false;
		[JsonPropertyName("cardType")]
		public SmartCardType CardType { get; set; } = SmartCardType.OctopusOnly;
		[JsonPropertyName("acceptUnregisteredCard")]
		public bool AcceptUnregisteredCard { get; set; } = false;
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
		// nullable for json's IgnoreNullValues (201106)
		[JsonPropertyName("inOutStrategy")]
		public InOutStrategy? DefaultInOut { get; set; } = InOutStrategy.SystemInOut;
		[JsonPropertyName("isEnableFx")]
		public bool[] IsEnableFx { get; set; } = new bool[] { true, false, true, false };
		[JsonPropertyName("dailyResetAutoInOut")]
		public bool DailyResetAutoInOut { get; set; } = true;
		[JsonPropertyName("dailyResetAutoInOutTime")]
		public string DailyResetAutoInOutTime { get; set; } = "00:00";
	}

	public enum AccessRight
	{
		System, RegisteredCard
	}

	public class RemoteDoorRelayControl
	{
		[JsonPropertyName("enabled")]
		public bool Enabled { get; set; } = true;
		[JsonPropertyName("id")]
		public int Id { get; set; } = 123;
		[JsonPropertyName("delayTimer")]
		public int DelayTimer { get; set; } = 3000;
		[JsonPropertyName("accessRight")]
		public AccessRight AccessRight { get; set; } = AccessRight.System;
	}

	public class DailyReboot
	{
		[JsonPropertyName("enabled")]
		public bool Enabled { get; set; } = true;
		[JsonPropertyName("time")]
		public string Time { get; set; } = "02:00";
	}

	public class TimeSync
	{
		[JsonPropertyName("timeZone")]
		public string TimeZone { get; set; } = "HK";
		[JsonPropertyName("timeServer")]
		public string TimeServer { get; set; } = "time.google.com";
		[JsonPropertyName("isEnableSNTP")]
		public bool IsEnableSNTP { get; set; } = true;
		[JsonPropertyName("isSyncMasterTime")]
		public bool IsSyncMasterTime { get; set; } = true;
	}
}
