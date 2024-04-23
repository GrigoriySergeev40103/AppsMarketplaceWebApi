using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AppsMarketplaceWebApi
{
	public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<User>(options)
	{
		public DbSet<App> Apps { get; set; }
		public DbSet<AppFile> AppFiles { get; set; }
		public DbSet<AppsOwnershipInfo> AppsOwnershipInfos { get; set; }
		public DbSet<AppCategory> AppCategories { get; set; }
		public DbSet<Comment> Comments { get; set; }
		public DbSet<AppPicture> AppPictures { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
		{
			base.OnConfiguring(optionsBuilder);
		}
	}
}
