using dotnet_core_web_client.Models;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Repository
{
	public interface ITerminalSettingsRepository
	{
		Task<TerminalSettingsDto> GetTerminalSettingsBySnAsync(string sn);
		Task<TerminalSettingsDto> UpsertTerminalSettingsAsync(TerminalSettingsDto terminslSettingsDto, string sn);
	}
}