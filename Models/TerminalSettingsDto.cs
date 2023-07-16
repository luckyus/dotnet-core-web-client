using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace dotnet_core_web_client.Models
{
	public class TerminalSettingsDto
	{
		[JsonPropertyName("terminalId")]
		public string TerminalId { get; set; }
		[JsonPropertyName("description")]
		public string Description { get; set; }
		[JsonPropertyName("language")]
		public string Language { get; set; }
		[JsonPropertyName("dateTimeFormat")]
		public string DateTimeFormat { get; set; }
		[JsonPropertyName("allowedOrigins")]
		public string[] AllowedOrigins { get; set; }
		[JsonPropertyName("inOutTrigger")]
		public SortedDictionary<string, InOutStatus> InOutTigger { get; set; }

		[JsonPropertyName("cameraControl")]
		public CameraControlDto CameraControl { get; set; }
		[JsonPropertyName("smartCardControl")]
		public SmartCardControlDto SmartCardControl { get; set; }
		[JsonPropertyName("inOutControl")]
		public InOutControlDto InOutControl { get; set; }

		[JsonPropertyName("localDoorRelayControl")]
		public LocalDoorRelayControlDto LocalDoorRelayControl { get; set; }
		[JsonPropertyName("remoteDoorRelayControl")]
		public RemoteDoorRelayControlDto RemoteDoorRelayControl { get; set; }
		[JsonPropertyName("dailyReboot")]
		public DailyRebootDto DailyReboot { get; set; }
		[JsonPropertyName("timeSync")]
		public TimeSyncDto TimeSync { get; set; }
		[JsonPropertyName("antiPassback")]
		public AntiPassbackDto AntiPassback { get; set; }
		[JsonPropertyName("dailySingleAccess")]
		public DailySingleAccessDto DailySingleAccess { get; set; }

		[JsonPropertyName("tempDetectEnabled")]
		public bool TempDetectEnable { get; set; }
		[JsonPropertyName("faceDetectEnabled")]
		public bool FaceDetectEnable { get; set; }
		[JsonPropertyName("flashLightEnabled")]
		public bool FlashLightEnabled { get; set; }
		[JsonPropertyName("tempCacheDuration")]
		public int TempCacheDuration { get; set; }
		[JsonPropertyName("autoUpdateEnabled")]
		public bool? AutoUpdateEnabled { get; set; }

		public static explicit operator TerminalSettingsDto(TerminalSettings terminalSettings)
		{
			if (terminalSettings == null)
			{
				return null;
			}

			TerminalSettingsDto dto = new()
			{
				TerminalId = terminalSettings.TerminalId,
				Description = terminalSettings.Description,
				Language = terminalSettings.Language,
				DateTimeFormat = terminalSettings.DateTimeFormat,
				AllowedOrigins = terminalSettings.AllowedOrigins,
				InOutTigger = terminalSettings.InOutTigger,

				TempDetectEnable = terminalSettings.TempDetectEnable,
				FaceDetectEnable = terminalSettings.FaceDetectEnable,
				FlashLightEnabled = terminalSettings.FlashLightEnabled,
				TempCacheDuration = terminalSettings.TempCacheDuration,
				AutoUpdateEnabled = terminalSettings.AutoUpdateEnabled,

				CameraControl = new CameraControlDto
				{
					Enable = terminalSettings.CameraControl.Enable,
					FrameRate = terminalSettings.CameraControl.FrameRate,
					Environment = terminalSettings.CameraControl.Environment,
					Resolution = terminalSettings.CameraControl.Resolution
				},

				SmartCardControl = new SmartCardControlDto
				{
					IsReadCardSNOnly = terminalSettings.SmartCardControl.IsReadCardSNOnly,
					CardType = terminalSettings.SmartCardControl.CardType,
					AcceptUnknownCard = terminalSettings.SmartCardControl.AcceptUnknownCard,
					AcceptUnregisteredCard = terminalSettings.SmartCardControl.AcceptUnregisteredCard
				},

				InOutControl = new InOutControlDto
				{
					DefaultInOut = terminalSettings.InOutControl.DefaultInOut,
					DailyResetAutoInOut = terminalSettings.InOutControl.DailyResetAutoInOut,
					DailyResetAutoInOutTime = terminalSettings.InOutControl.DailyResetAutoInOutTime,
					IsEnableFx = terminalSettings.InOutControl.IsEnableFx
				},

				LocalDoorRelayControl = new LocalDoorRelayControlDto
				{
					DoorRelayStatus = terminalSettings.LocalDoorRelayControl.DoorRelayStatus,
					Duration = terminalSettings.LocalDoorRelayControl.Duration
				},

				RemoteDoorRelayControl = new RemoteDoorRelayControlDto
				{
					Enabled = terminalSettings.RemoteDoorRelayControl.Enabled,
					Id = terminalSettings.RemoteDoorRelayControl.Id,
					DelayTimer = terminalSettings.RemoteDoorRelayControl.DelayTimer,
					AccessRight = terminalSettings.RemoteDoorRelayControl.AccessRight
				},

				DailyReboot = new DailyRebootDto
				{
					Enabled = terminalSettings.DailyReboot.Enabled,
					Time = terminalSettings.DailyReboot.Time
				},

				TimeSync = new TimeSyncDto
				{
					TimeZone = terminalSettings.TimeSync.TimeZone,
					TimeOffSet = terminalSettings.TimeSync.TimeOffSet,
					TimeServer = terminalSettings.TimeSync.TimeServer,
					IsEnableSNTP = terminalSettings.TimeSync.IsEnableSNTP,
					IsSyncMasterTime = terminalSettings.TimeSync.IsSyncMasterTime
				},

				AntiPassback = new AntiPassbackDto
				{
					Type = terminalSettings.AntiPassback.Type,
					IsDailyReset = terminalSettings.AntiPassback.IsDailyReset,
					DailyResetTime = terminalSettings.AntiPassback.DailyResetTime
				},

				DailySingleAccess = new DailySingleAccessDto
				{
					Type = terminalSettings.DailySingleAccess.Type,
					IsDailyReset = terminalSettings.DailySingleAccess.IsDailyReset,
					DailyResetTime = terminalSettings.DailySingleAccess.DailyResetTime
				},
			};

			return dto;
		}
	}

	/// <summary>
	/// only for iGuard540, not for iGuardExpress540 (230423)
	/// </summary>
	public class AntiPassbackDto
	{
		[JsonPropertyName("type")]
		public string Type { get; set; }
		[JsonPropertyName("isDailyReset")]
		public bool IsDailyReset { get; set; }
		[JsonPropertyName("dailyResetTime")]
		public string DailyResetTime { get; set; }
	}

	/// <summary>
	/// only for iGuard540, not for iGuardExpress540 (230423)
	/// </summary>
	public class DailySingleAccessDto
	{
		[JsonPropertyName("type")]
		public string Type { get; set; }
		[JsonPropertyName("isDailyReset")]
		public bool IsDailyReset { get; set; }
		[JsonPropertyName("dailyResetTime")]
		public string DailyResetTime { get; set; }
	}

	public class CameraControlDto
	{
		[JsonPropertyName("enable")]
		public bool Enable { get; set; }
		[JsonPropertyName("frameRate")]
		public int? FrameRate { get; set; }
		[JsonPropertyName("environment")]
		public CameraEnvironment? Environment { get; set; }
		[JsonPropertyName("resolution")]
		public CameraResolution? Resolution { get; set; }
	}

	public class SmartCardControlDto
	{
		[JsonPropertyName("isReadCardSNOnly")]
		public bool IsReadCardSNOnly { get; set; } = false;
		[JsonPropertyName("cardType")]
		public SmartCardType CardType { get; set; } = SmartCardType.OctopusOnly;
		[JsonPropertyName("acceptUnknownCard")]
		public bool? AcceptUnknownCard { get; set; } = false;
		[JsonPropertyName("acceptUnregisteredCard")]
		public bool? AcceptUnregisteredCard { get; set; }
	}

	public class InOutControlDto
	{
		// nullable for json's IgnoreNullValues (201106)
		[JsonPropertyName("inOutStrategy")]
		public InOutStrategy? DefaultInOut { get; set; } = InOutStrategy.SystemInOut;
		[JsonPropertyName("isEnableFx")]
		public bool[] IsEnableFx { get; set; } = new bool[] { true, false, true, false };
		[JsonPropertyName("dailyResetEnabled")]
		public bool? DailyResetAutoInOut { get; set; }
		[JsonPropertyName("dailyResetTime")]
		public string DailyResetAutoInOutTime { get; set; }
	}

	public class LocalDoorRelayControlDto
	{
		// added for iGuard540 (230331)
		[JsonPropertyName("openDoorStatus")]
		public DoorRelayStatus DoorRelayStatus { get; set; } = new DoorRelayStatus() { In = true };
		[JsonPropertyName("delayTimer")]
		public int Duration { get; set; } = 3000;
	}

	public class DoorRelayStatusDto
	{
		public bool? In { get; set; }
		public bool? Out { get; set; }
		public bool? F1toF4 { get; set; }
	}

	public class RemoteDoorRelayControlDto
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

	public class DailyRebootDto
	{
		[JsonPropertyName("enabled")]
		public bool Enabled { get; set; } = true;
		[JsonPropertyName("time")]
		public string Time { get; set; } = "02:00";
	}

	public class TimeSyncDto
	{
		[JsonPropertyName("timeZone")]
		public string TimeZone { get; set; } = "HK";
		[JsonPropertyName("timeOffSet")]
		public decimal TimeOffSet { get; set; }
		[JsonPropertyName("timeServer")]
		public string TimeServer { get; set; } = "time.google.com";
		[JsonPropertyName("isEnableSNTP")]
		public bool IsEnableSNTP { get; set; } = true;
		[JsonPropertyName("isSyncMasterTime")]
		public bool IsSyncMasterTime { get; set; } = true;
	}
}
