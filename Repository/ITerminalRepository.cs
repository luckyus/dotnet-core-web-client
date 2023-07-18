using dotnet_core_web_client.Models;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Repository
{
	public interface ITerminalRepository
	{
		Task<TerminalsDto> GetTerminalsBySnAsync(string sn);
		Task<TerminalsDto> UpsertTerminalsAsync(TerminalsDto terminalsDto);
	}
}