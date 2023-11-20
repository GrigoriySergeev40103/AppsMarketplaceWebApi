using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppsMarketplaceWebApi
{
	public class AppDbContext(DbContextOptions<AppDbContext> options) : 
		IdentityDbContext<User>(options)
	{
	}
}
