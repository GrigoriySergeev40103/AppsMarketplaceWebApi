using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppsMarketplaceWebApi
{
	public class AppDbContext : IdentityDbContext<User>
	{
		public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
		{

		}

		public AppDbContext()
		{

		}

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);
			optionsBuilder.UseMySql("server=127.0.0.1;port=3306;user=root;password=sdas13Lc30589z[1;database=app_market",
				ServerVersion.AutoDetect("server=127.0.0.1;port=3306;user=root;password=sdas13Lc30589z[1;database=app_market"));
		}
	}
}
