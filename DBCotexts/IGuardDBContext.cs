using Microsoft.EntityFrameworkCore;

namespace dotnet_core_web_client.DBCotexts
{
	public class IGuardDBContext : DbContext
	{
        public IGuardDBContext(DbContextOptions<IGuardDBContext> options) : base(options)
        {            
        }

		public DbSet<Models.Network> Network { get; set; }
		public DbSet<Models.Terminal> Terminal { get; set; }
		public DbSet<Models.TerminalSettings> TerminalSettings { get; set; }
    }
}
