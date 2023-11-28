﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AppsMarketplaceWebApi.Models
{
	[PrimaryKey(nameof(AppId), nameof(UserId))]
	public class AppsOwnershipInfo
	{
		public string AppId { get; set; } = null!;

		public string UserId { get; set; } = null!;
	}
}
