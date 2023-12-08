using Microsoft.AspNetCore.Identity;

namespace AppsMarketplaceWebApi
{
	public class User : IdentityUser
	{
		public string PathToAvatarPic { get; set; } = null!;

		public decimal Balance { get; set; }
	}
}
