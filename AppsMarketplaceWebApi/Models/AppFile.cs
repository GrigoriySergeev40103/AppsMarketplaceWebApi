using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AppsMarketplaceWebApi.Models
{
	public class AppFile
	{
		[Key]
		public string AppFileId { get; set; } = null!;

		public string AppId { get; set; } = null!;

		public string Filename { get; set; } = null!;

		public string Path { get; set; } = null!;

		public string Extension { get; set; } = null!;
	}
}
