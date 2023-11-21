using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AppsMarketplaceWebApi.Models
{
	[PrimaryKey(nameof(AppId), nameof(UserId))]
	public class AppsOwnershipInfo
	{
		public int AppId { get; set; }

		public string UserId { get; set; } = null!;
	}
}
