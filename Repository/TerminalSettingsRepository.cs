using dotnet_core_web_client.DBCotexts;
using dotnet_core_web_client.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Repository
{
	public class TerminalSettingsRepository : ITerminalSettingsRepository
	{
		private readonly IGuardDBContext _dBContext;

		public TerminalSettingsRepository(IGuardDBContext dBContext)
		{
			_dBContext = dBContext;
		}

		public async Task<TerminalSettingsDto> GetTerminalSettingsBySnAsync(string sn)
		{
			TerminalSettings terminalSettings = await _dBContext.TerminalSettings.FirstOrDefaultAsync(x => x.SN == sn);
			return (TerminalSettingsDto)terminalSettings;
		}

		public async Task<TerminalSettingsDto> UpsertTerminalSettingsAsync(TerminalSettingsDto terminalSettingsDto, string sn)
		{
			TerminalSettings terminalSettings = (TerminalSettings)terminalSettingsDto;
			terminalSettings.SN = sn;

			try
			{
				TerminalSettings existing = await _dBContext.TerminalSettings.FirstOrDefaultAsync(x => x.SN == sn);

				if (existing == null)
				{
					_dBContext.TerminalSettings.Add(terminalSettings);
				}
				else
				{
					existing.TerminalId = terminalSettings.TerminalId;
					existing.Description = terminalSettings.Description;
					existing.Language = terminalSettings.Language;
					existing.DateTimeFormat = terminalSettings.DateTimeFormat;
					existing.TempDetectEnable = terminalSettings.TempDetectEnable;
					existing.FaceDetectEnable = terminalSettings.FaceDetectEnable;
					existing.FlashLightEnabled = terminalSettings.FlashLightEnabled;
					existing.TempCacheDuration = terminalSettings.TempCacheDuration;
					existing.AutoUpdateEnabled = terminalSettings.AutoUpdateEnabled;
					existing.AllowedOriginsStr = terminalSettings.AllowedOriginsStr;
					existing.CameraControlStr = terminalSettings.CameraControlStr;
					existing.SmartCardControlStr = terminalSettings.SmartCardControlStr;
					existing.InOutControlStr = terminalSettings.InOutControlStr;
					existing.InOutTiggerStr = terminalSettings.InOutTiggerStr;
					existing.LocalDoorRelayControlStr = terminalSettings.LocalDoorRelayControlStr;
					existing.RemoteDoorRelayControlStr = terminalSettings.RemoteDoorRelayControlStr;
					existing.DailyRebootStr = terminalSettings.DailyRebootStr;
					existing.TimeSyncStr = terminalSettings.TimeSyncStr;
					existing.AntiPassbackStr = terminalSettings.AntiPassbackStr;
					existing.DailySingleAccessStr = terminalSettings.DailySingleAccessStr;

					_dBContext.TerminalSettings.Update(existing);
				}

				await _dBContext.SaveChangesAsync();

				return (TerminalSettingsDto)terminalSettings;
			}
			catch
			{
				return null;
			}
		}
	}
}
