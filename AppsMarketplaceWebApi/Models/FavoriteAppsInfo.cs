using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AppsMarketplaceWebApi.Models
{
	[PrimaryKey(nameof(AppId), nameof(UserId))]
	public class FavoriteAppsInfo
	{
		public string AppId { get; set; } = null!;

		public string UserId { get; set; } = null!;
	}
}
