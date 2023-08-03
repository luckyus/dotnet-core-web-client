using dotnet_core_web_client.DBCotexts;
using dotnet_core_web_client.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Repository
{
	public class TerminalRepository : BaseRepository, ITerminalRepository
	{
		private readonly IGuardDBContext _dBContext;
		private readonly string cacheKeyPrefix = "Terminals_";

		public TerminalRepository(IGuardDBContext dBContext, IMemoryCache memoryCache) : base(memoryCache)
		{
			_dBContext = dBContext;
		}

		public async Task<TerminalsDto> GetTerminalsBySnAsync(string sn)
		{
			string key = $"{cacheKeyPrefix}{sn}";

			if (_memoryCache.TryGetValue(key, out TerminalsDto terminalsDto) && terminalsDto != null)
			{
				return terminalsDto;
			}

			Terminals terminals = await _dBContext.Terminals.AsNoTracking().FirstOrDefaultAsync(x => x.SN == sn);

			if (terminals == null)
			{
				terminalsDto = new TerminalsDto { SN = sn };

				terminals = (Terminals)terminalsDto;
				_dBContext.Terminals.Add(terminals);
				await _dBContext.SaveChangesAsync();
			}
			else
			{
				terminalsDto = (TerminalsDto)terminals;
			}

			SetCache(key, terminalsDto);
			return terminalsDto;
		}

		public async Task<TerminalsDto> UpsertTerminalsAsync(TerminalsDto terminalsDto)
		{
			Terminals terminals = (Terminals)terminalsDto;

			try
			{
				Terminals existing = await _dBContext.Terminals.FirstOrDefaultAsync(x => x.SN == terminals.SN);

				if (existing == null)
				{
					_dBContext.Terminals.Add(terminals);
				}
				else
				{
					existing.FirmwareVersion = terminals.FirmwareVersion;
					existing.HasRS485 = terminals.HasRS485;
					existing.MasterServer = terminals.MasterServer;
					existing.PhotoServer = terminals.PhotoServer;
					existing.SupportedCardType = terminals.SupportedCardType;
					existing.RegDate = terminals.RegDate;
					existing.Environment = terminals.Environment;

					_dBContext.Terminals.Update(existing);
				}

				ResetCache(cacheKeyPrefix);
				await _dBContext.SaveChangesAsync();

				return (TerminalsDto)terminals;
			}
			catch
			{
				return null;
			}
		}
	}
}
