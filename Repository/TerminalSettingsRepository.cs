using dotnet_core_web_client.DBCotexts;
using dotnet_core_web_client.Models;
using Microsoft.EntityFrameworkCore;
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

			TerminalSettings existing = await _dBContext.TerminalSettings.FirstOrDefaultAsync(x => x.SN == sn);

			if (existing == null)
			{
				_dBContext.TerminalSettings.Add(terminalSettings);
			}
			else
			{
				_dBContext.TerminalSettings.Update(terminalSettings);
			}

			await _dBContext.SaveChangesAsync();

			return (TerminalSettingsDto)terminalSettings;
		}
	}
}
