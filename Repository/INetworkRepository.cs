using dotnet_core_web_client.Models;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Repository
{
	public interface INetworkRepository
	{
		Task<NetworksDto> GetNetworkBySnAsync(string sn);
		Task<NetworksDto> UpsertNetworkAsync(NetworksDto networksDto, string sn);
	}
}