using System.ComponentModel.DataAnnotations;

namespace AppsMarketplaceWebApi.Models
{
	public class App
	{
		[Key]
		public string AppId { get; set; } = null!;

		public string DeveloperId { get; set; } = null!;

		public string Name { get; set; } = null!;

		public string AppPicturePath { get;set; } = null!;

		public string Extension { get; set; } = null!;

		public DateTime UploadDate { get; set; }

		public decimal Price { get; set; }

		public string CategoryName { get; set; } = null!;

		public string? Description { get; set; }

		public string? SpecialDescription { get; set; }
	}
}
