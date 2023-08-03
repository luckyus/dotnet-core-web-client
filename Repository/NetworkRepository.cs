using dotnet_core_web_client.DBCotexts;
using dotnet_core_web_client.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Repository
{
	public class NetworkRepository : BaseRepository, INetworkRepository
	{
		private readonly IGuardDBContext _dBContext;
		private readonly string cacheKeyPrefix = "Network_";

		public NetworkRepository(IGuardDBContext dBContext, IMemoryCache memoryCache) : base(memoryCache)
		{
			_dBContext = dBContext;
		}

		public async Task<NetworksDto> GetNetworkBySnAsync(string sn)
		{
			string key = $"{cacheKeyPrefix}{sn}";

			if (_memoryCache.TryGetValue(key, out NetworksDto networksDto))
			{
				return networksDto;
			}

			Networks networks = await _dBContext.Networks.FirstOrDefaultAsync(x => x.SN == sn);

			if (networks == null)
			{
				networksDto = new NetworksDto();

				networks = (Networks)networksDto;
				networks.SN = sn;
				_dBContext.Networks.Add(networks);
				await _dBContext.SaveChangesAsync();
			}
			else
			{
				networksDto = (NetworksDto)networks;
			}

			SetCache(key, networksDto);
			return networksDto;
		}

		public async Task<NetworksDto> UpsertNetworkAsync(NetworksDto networksDto, string sn)
		{
			Networks networks = (Networks)networksDto;
			networks.SN = sn;

			try
			{
				Networks existing = await _dBContext.Networks.FirstOrDefaultAsync(x => x.SN == networks.SN);

				if (existing == null)
				{
					_dBContext.Networks.Add(networks);
				}
				else
				{
					existing.Interface = networks.Interface;
					existing.Ip = networks.Ip;
					existing.Port = networks.Port;
					existing.SslPort = networks.SslPort;
					existing.SubnetMask = networks.SubnetMask;
					existing.Gateway = networks.Gateway;
					existing.Dns = networks.Dns;
					existing.Ssid = networks.Ssid;
					existing.Password = networks.Password;
					existing.IsWireless = networks.IsWireless;
					existing.IsDhcp = networks.IsDhcp;

					_dBContext.Networks.Update(existing);
				}

				await _dBContext.SaveChangesAsync();
				ResetCache(cacheKeyPrefix);

				return (NetworksDto)networks;
			}
			catch
			{
				return null;
			}
		}
	}
}
