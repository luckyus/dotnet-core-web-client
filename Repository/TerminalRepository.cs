using dotnet_core_web_client.DBCotexts;
using dotnet_core_web_client.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace dotnet_core_web_client.Repository
{
	public class TerminalRepository : ITerminalRepository
	{
		private readonly IGuardDBContext _dBContext;

		public TerminalRepository(IGuardDBContext dBContext)
		{
			_dBContext = dBContext;
		}

		public async Task<TerminalsDto> GetTerminalsBySnAsync(string sn)
		{
			Terminals terminals = await _dBContext.Terminals.FirstOrDefaultAsync(x => x.SN == sn);
			return (TerminalsDto)terminals;
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
					existing.SN = terminals.SN;
					existing.FirmwareVersion = terminals.FirmwareVersion;
					existing.HasRS485 = terminals.HasRS485;
					existing.MasterServer = terminals.MasterServer;
					existing.PhotoServer = terminals.PhotoServer;
					existing.SupportedCardType = terminals.SupportedCardType;
					existing.RegDate = terminals.RegDate;
					existing.Environment = terminals.Environment;

					_dBContext.Terminals.Update(existing);
				}

				await _dBContext.SaveChangesAsync();

				return (TerminalsDto)terminals;
			}
			catch (DbUpdateException ex)
			{
				throw ex;
			}
		}
	}
}
