using System.ComponentModel.DataAnnotations;

namespace AppsMarketplaceWebApi.Models
{
	public class Comment
	{
		[Key]
		public string CommentId { get; set; } = null!;

		/// <summary>
		/// An Id of an app that a comment was left on
		/// </summary>
		public string AppId { get; set; } = null!;

		/// <summary>
		/// Id of a user who left the comment
		/// </summary>
		public string CommenteeId { get; set; } = null!;

		/// <summary>
		/// The text of a comment(named 'Content' since i guess it could also have emojis or whatever in them)
		/// </summary>
		public string CommentContent { get; set; } = null!;

		public DateTime UploadDate { get; set; }
	}
}
