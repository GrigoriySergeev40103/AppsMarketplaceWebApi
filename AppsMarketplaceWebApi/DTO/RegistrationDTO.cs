namespace AppsMarketplaceWebApi.DTO
{
	public class RegistrationDTO
	{
		/// <summary>
		/// The user's name.
		/// </summary>
		public required string Username { get; init; }

		/// <summary>
		/// The user's email address.
		/// </summary>
		public required string Email { get; init; }

		/// <summary>
		/// The user's password.
		/// </summary>
		public required string Password { get; init; }
	}
}
