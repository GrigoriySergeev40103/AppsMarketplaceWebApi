﻿using System.ComponentModel.DataAnnotations;

namespace AppsMarketplaceWebApi.Models
{
	public class App
	{
		[Key]
		public string AppId { get; set; } = null!;

		public string DeveloperId { get; set; } = null!;

		public string Name { get; set; } = null!;

		public string AppMainPicPath { get;set; } = null!;

		public DateTime UploadDate { get; set; }

		public string CategoryName { get; set; } = null!;

		public string? Description { get; set; }

		public string? SpecialDescription { get; set; }
	}
}
