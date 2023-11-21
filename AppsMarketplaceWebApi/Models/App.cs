namespace AppsMarketplaceWebApi.Models
{
	public class App
	{
		public int AppId { get; set; }

		public string DeveloperId { get; set; } = null!;

		public string Name { get; set; } = null!;

		public string Path { get; set; } = null!;

		public DateTime UploadDate { get; set; }

		public decimal Price { get; set; }

		public int CategoryId { get; set; }

		public string? Description { get; set; }

		public string? SpecialDescription { get; set; }
	}
}
