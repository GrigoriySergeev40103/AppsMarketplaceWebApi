﻿using Microsoft.AspNetCore.Identity;

namespace AppsMarketplaceWebApi
{
	public class User : IdentityUser
	{
		public string DisplayName { get; set; } = null!;

		public string PathToAvatarPic { get; set; } = null!;
	}
}
