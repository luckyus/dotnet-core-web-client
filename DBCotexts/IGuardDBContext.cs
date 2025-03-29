using dotnet_core_web_client.Models;
using Microsoft.EntityFrameworkCore;

namespace dotnet_core_web_client.DBCotexts
{
	public class IGuardDBContext(DbContextOptions<IGuardDBContext> options) : DbContext(options)
	{
		public DbSet<TerminalSettings> TerminalSettings { get; set; }
		public DbSet<Terminals> Terminals { get; set; }
		public DbSet<Networks> Networks { get; set; }
	}
}
