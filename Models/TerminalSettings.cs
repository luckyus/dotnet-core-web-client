using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Models
{
	public class TerminalSettings
	{
		[Key]
		public string SN { get; set; }
		public string TerminalId { get; set; }
		public string Description { get; set; }
		public string Language { get; set; }
		public string DateTimeFormat { get; set; }
		public bool TempDetectEnable { get; set; }
		public bool FaceDetectEnable { get; set; }
		public bool FlashLightEnabled { get; set; }
		public int TempCacheDuration { get; set; }
		public bool? AutoUpdateEnabled { get; set; }
		public string AllowedOriginsStr { get; set; }
		[NotMapped]
		public string[] AllowedOrigins
		{
			get { return AllowedOriginsStr.Split(','); }
			set { AllowedOriginsStr = string.Join(",", value); }
		}
		public string CameraControlStr { get; set; }
		[NotMapped]
		public CameraControl CameraControl
		{
			get { return JsonSerializer.Deserialize<CameraControl>(CameraControlStr); }
			set { CameraControlStr = JsonSerializer.Serialize<CameraControl>(value); }
		}
		public string FaceRecognitionControlStr { get; set; } = "{}";
		[NotMapped]
		public FaceRecognitionControl FaceRecognitionControl
		{
			get { return JsonSerializer.Deserialize<FaceRecognitionControl>(FaceRecognitionControlStr); }
			set { FaceRecognitionControlStr = JsonSerializer.Serialize<FaceRecognitionControl>(value); }
		}
		public string SmartCardControlStr { get; set; }
		[NotMapped]
		public SmartCardControl SmartCardControl
		{
			get { return JsonSerializer.Deserialize<SmartCardControl>(SmartCardControlStr); }
			set { SmartCardControlStr = JsonSerializer.Serialize(value); }
		}
		public string InOutControlStr { get; set; }
		[NotMapped]
		public InOutControl InOutControl
		{
			get { return JsonSerializer.Deserialize<InOutControl>(InOutControlStr); }
			set { InOutControlStr = JsonSerializer.Serialize(value); }
		}
		public string InOutTiggerStr { get; set; }
		[NotMapped]
		public SortedDictionary<string, InOutStatus> InOutTigger
		{
			get { return JsonSerializer.Deserialize<SortedDictionary<string, InOutStatus>>(InOutTiggerStr); }
			set { InOutTiggerStr = JsonSerializer.Serialize(value); }
		}
		public string LocalDoorRelayControlStr { get; set; }
		[NotMapped]
		public LocalDoorRelayControl LocalDoorRelayControl
		{
			get { return JsonSerializer.Deserialize<LocalDoorRelayControl>(LocalDoorRelayControlStr); }
			set { LocalDoorRelayControlStr = JsonSerializer.Serialize(value); }
		}
		public string RemoteDoorRelayControlStr { get; set; }
		[NotMapped]
		public RemoteDoorRelayControl RemoteDoorRelayControl
		{
			get { return JsonSerializer.Deserialize<RemoteDoorRelayControl>(RemoteDoorRelayControlStr); }
			set { RemoteDoorRelayControlStr = JsonSerializer.Serialize(value); }
		}
		public string DailyRebootStr { get; set; }
		[NotMapped]
		public DailyReboot DailyReboot
		{
			get { return JsonSerializer.Deserialize<DailyReboot>(DailyRebootStr); }
			set { DailyRebootStr = JsonSerializer.Serialize(value); }
		}
		public string TimeSyncStr { get; set; }
		[NotMapped]
		public TimeSync TimeSync
		{
			get { return JsonSerializer.Deserialize<TimeSync>(TimeSyncStr); }
			set { TimeSyncStr = JsonSerializer.Serialize(value); }
		}
		public string AntiPassbackStr { get; set; }     // only for iGuard540, not for iGuardExpress540 (230423)
		[NotMapped]
		public AntiPassback AntiPassback
		{
			get { return JsonSerializer.Deserialize<AntiPassback>(AntiPassbackStr); }
			set { AntiPassbackStr = JsonSerializer.Serialize(value); }
		}
		public string DailySingleAccessStr { get; set; }    // ditto (230423)
		[NotMapped]
		public DailySingleAccess DailySingleAccess
		{
			get { return JsonSerializer.Deserialize<DailySingleAccess>(DailySingleAccessStr); }
			set { DailySingleAccessStr = JsonSerializer.Serialize(value); }
		}

		public static explicit operator TerminalSettings(TerminalSettingsDto terminalSettingsDto)
		{
			TerminalSettings terminalSettings = new()
			{
				TerminalId = terminalSettingsDto.TerminalId,
				Description = terminalSettingsDto.Description,
				Language = terminalSettingsDto.Language,
				DateTimeFormat = terminalSettingsDto.DateTimeFormat,
				TempDetectEnable = terminalSettingsDto.TempDetectEnable,
				FaceDetectEnable = terminalSettingsDto.FaceDetectEnable,
				FlashLightEnabled = terminalSettingsDto.FlashLightEnabled,
				TempCacheDuration = terminalSettingsDto.TempCacheDuration,
				AutoUpdateEnabled = terminalSettingsDto.AutoUpdateEnabled,
				AllowedOrigins = terminalSettingsDto.AllowedOrigins,
				InOutTigger = terminalSettingsDto.InOutTigger,

				InOutControl = new InOutControl
				{
					DefaultInOut = terminalSettingsDto.InOutControl.DefaultInOut,
					DailyResetAutoInOut = terminalSettingsDto.InOutControl.DailyResetAutoInOut,
					DailyResetAutoInOutTime = terminalSettingsDto.InOutControl.DailyResetAutoInOutTime,
					IsEnableFx = terminalSettingsDto.InOutControl.IsEnableFx
				},

				LocalDoorRelayControl = new LocalDoorRelayControl
				{
					DoorRelayStatus = terminalSettingsDto.LocalDoorRelayControl.DoorRelayStatus,
					Duration = terminalSettingsDto.LocalDoorRelayControl.Duration
				},

				RemoteDoorRelayControl = new RemoteDoorRelayControl
				{
					Enabled = terminalSettingsDto.RemoteDoorRelayControl.Enabled,
					Id = terminalSettingsDto.RemoteDoorRelayControl.Id,
					DelayTimer = terminalSettingsDto.RemoteDoorRelayControl.DelayTimer,
					AccessRight = terminalSettingsDto.RemoteDoorRelayControl.AccessRight
				},

				DailyReboot = new DailyReboot
				{
					Enabled = terminalSettingsDto.DailyReboot.Enabled,
					Time = terminalSettingsDto.DailyReboot.Time
				},

				TimeSync = new TimeSync
				{
					TimeZone = terminalSettingsDto.TimeSync.TimeZone,
					TimeOffSet = terminalSettingsDto.TimeSync.TimeOffSet,
					TimeServer = terminalSettingsDto.TimeSync.TimeServer,
					IsEnableSNTP = terminalSettingsDto.TimeSync.IsEnableSNTP,
					IsSyncMasterTime = terminalSettingsDto.TimeSync.IsSyncMasterTime
				},

				AntiPassback = new AntiPassback
				{
					Type = terminalSettingsDto.AntiPassback.Type,
					IsDailyReset = terminalSettingsDto.AntiPassback.IsDailyReset,
					DailyResetTime = terminalSettingsDto.AntiPassback.DailyResetTime
				},

				DailySingleAccess = new DailySingleAccess
				{
					Type = terminalSettingsDto.DailySingleAccess.Type,
					IsDailyReset = terminalSettingsDto.DailySingleAccess.IsDailyReset,
					DailyResetTime = terminalSettingsDto.DailySingleAccess.DailyResetTime
				},

				CameraControl = new CameraControl
				{
					Enable = terminalSettingsDto.CameraControl.Enable,
					FrameRate = terminalSettingsDto.CameraControl.FrameRate,
					Environment = terminalSettingsDto.CameraControl.Environment,
					Resolution = terminalSettingsDto.CameraControl.Resolution
				},

				SmartCardControl = new SmartCardControl
				{
					IsReadCardSNOnly = terminalSettingsDto.SmartCardControl.IsReadCardSNOnly,
					CardType = terminalSettingsDto.SmartCardControl.CardType,
					AcceptUnknownCard = terminalSettingsDto.SmartCardControl.AcceptUnknownCard,
					AcceptUnregisteredCard = terminalSettingsDto.SmartCardControl.AcceptUnregisteredCard
				},

				FaceRecognitionControl = new FaceRecognitionControl
				{
					Enable = terminalSettingsDto.FaceRecognitionControl.Enable,
					AllowMultipleMatch = terminalSettingsDto.FaceRecognitionControl.AllowMultipleMatch,
					FaceDetectSensitivity = terminalSettingsDto.FaceRecognitionControl.FaceDetectConf,
					FaceRecognitionSecurity = terminalSettingsDto.FaceRecognitionControl.FaceRegConf,
					SnapShotInterval = terminalSettingsDto.FaceRecognitionControl.DetectInterval,
					ReAuthenticationDelay = terminalSettingsDto.FaceRecognitionControl.RecognitionInterval,
					ViewAngle = terminalSettingsDto.FaceRecognitionControl.CropSize
				}
			};

			return terminalSettings;
		}
	}

	/// <summary>
	/// only for iGuard540, not for iGuardExpress540 (230423)
	/// </summary>
	public class AntiPassback
	{
		public string Type { get; set; }
		public bool IsDailyReset { get; set; }
		public string DailyResetTime { get; set; }
	}

	/// <summary>
	/// only for iGuard540, not for iGuardExpress540 (230423)
	/// </summary>
	public class DailySingleAccess
	{
		public string Type { get; set; }
		public bool IsDailyReset { get; set; }
		public string DailyResetTime { get; set; }
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
		public bool Enable { get; set; }
		public int? FrameRate { get; set; }
		public CameraEnvironment? Environment { get; set; }
		public CameraResolution? Resolution { get; set; }
	}

	public class FaceRecognitionControl
	{
		public bool Enable { get; set; }
		public bool AllowMultipleMatch { get; set; } = false;
		public double FaceDetectSensitivity { get; set; } = 0.1;
		public double FaceRecognitionSecurity { get; set; } = 0.1;
		public double SnapShotInterval { get; set; } = 1.5;
		public int ReAuthenticationDelay { get; set; } = 3;
		public int ViewAngle { get; set; } = 130;
	}


	public enum SmartCardType
	{
		MifareOnly, OctopusOnly, MifareAndOctopus
	}

	public class SmartCardControl
	{
		public bool IsReadCardSNOnly { get; set; } = false;
		public SmartCardType CardType { get; set; } = SmartCardType.OctopusOnly;
		public bool? AcceptUnknownCard { get; set; } = false;
		public bool? AcceptUnregisteredCard { get; set; }
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
		public InOutStrategy? DefaultInOut { get; set; } = InOutStrategy.SystemInOut;
		public bool[] IsEnableFx { get; set; } = new bool[] { true, false, true, false };
		public bool? DailyResetAutoInOut { get; set; }
		public string DailyResetAutoInOutTime { get; set; }
	}

	public enum AccessRight
	{
		System, RegisteredCard
	}

	public class LocalDoorRelayControl
	{
		// added for iGuard540 (230331)
		public DoorRelayStatus DoorRelayStatus { get; set; } = new DoorRelayStatus() { In = true };
		public int Duration { get; set; } = 3000;
	}

	public class DoorRelayStatus
	{
		public bool? In { get; set; }
		public bool? Out { get; set; }
		public bool? F1toF4 { get; set; }
	}

	public class RemoteDoorRelayControl
	{
		public bool Enabled { get; set; } = true;
		public int Id { get; set; } = 123;
		public int DelayTimer { get; set; } = 3000;
		public AccessRight AccessRight { get; set; } = AccessRight.System;
	}

	public class DailyReboot
	{
		public bool Enabled { get; set; } = true;
		public string Time { get; set; } = "02:00";
	}

	public class TimeSync
	{
		public string TimeZone { get; set; } = "HK";
		public decimal TimeOffSet { get; set; }
		public string TimeServer { get; set; } = "time.google.com";
		public bool IsEnableSNTP { get; set; } = true;
		public bool IsSyncMasterTime { get; set; } = true;
	}
}
