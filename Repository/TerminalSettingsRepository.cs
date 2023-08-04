using dotnet_core_web_client.DBCotexts;
using dotnet_core_web_client.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Repository
{
	public class TerminalSettingsRepository : BaseRepository, ITerminalSettingsRepository
	{
		private readonly IGuardDBContext _dBContext;
		private readonly string cacheKeyPrefix = "TerminalSettings_";

		public TerminalSettingsRepository(IGuardDBContext dBContext, IMemoryCache memoryCache) : base(memoryCache)
		{
			_dBContext = dBContext;
		}

		public async Task<TerminalSettingsDto> GetTerminalSettingsBySnAsync(string sn)
		{
			string key = $"{cacheKeyPrefix}{sn}";

			if (_memoryCache.TryGetValue(key, out TerminalSettingsDto terminalSettingsDto) && terminalSettingsDto != null)
			{
				return terminalSettingsDto;
			}

			TerminalSettings terminalSettings = await _dBContext.Set<TerminalSettings>().AsNoTracking().FirstOrDefaultAsync(x => x.SN == sn);

			if (terminalSettings == null)
			{
				terminalSettingsDto = new TerminalSettingsDto();

				terminalSettings = (TerminalSettings)terminalSettingsDto;
				terminalSettings.SN = sn;

				_dBContext.TerminalSettings.Add(terminalSettings);
				await _dBContext.SaveChangesAsync();
			}
			else
			{
				terminalSettingsDto = (TerminalSettingsDto)terminalSettings;
			}

			SetCache(key, terminalSettingsDto);
			return terminalSettingsDto;
		}

		public async Task<TerminalSettingsDto> UpsertTerminalSettingsAsync(TerminalSettingsDto terminalSettingsDto, string sn)
		{
			string key = $"{cacheKeyPrefix}{sn}";

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

				ResetCache(cacheKeyPrefix);
				await _dBContext.SaveChangesAsync();

				return (TerminalSettingsDto)terminalSettings;
			}
			catch (Exception ex)
			{
				return null;
			}
		}
	}
}
