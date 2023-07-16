using dotnet_core_web_client.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_core_web_client.DBCotexts
{
	public class IGuardDBContext : DbContext
	{
		public IGuardDBContext(DbContextOptions<IGuardDBContext> options) : base(options)
		{
		}

		public DbSet<TerminalSettings> TerminalSettings { get; set; }
		// public DbSet<Models.Network> Network { get; set; }
		// public DbSet<Models.Terminal> Terminal { get; set; }
	}
}
