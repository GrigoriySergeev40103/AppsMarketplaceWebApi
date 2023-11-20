using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppsMarketplaceWebApi
{
	public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User>(options)
	{
		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);
		}
	}
}
