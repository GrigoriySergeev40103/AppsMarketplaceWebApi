using System.ComponentModel.DataAnnotations;

namespace AppsMarketplaceWebApi.Models
{
	public class AppPicture
	{
		[Key]
		public string PictureId { get; set; } = null!;

		public string AppId { get; set; } = null!;

		public string Path { get; set; } = null!;

		public DateTime UploadDate { get; set; }
	}
}
