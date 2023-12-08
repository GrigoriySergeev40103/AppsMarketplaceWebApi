namespace AppsMarketplaceWebApi.DTO
{
	/// <summary>
	/// A more detailed DTO of a user meant to be sent only to themselfes
	/// </summary>
	public class PersonalUserDTO
	{
		public string Id { get; set; } = default!;

		public string? UserName { get; set; }

		public decimal Balance { get; set; }
	}
}
