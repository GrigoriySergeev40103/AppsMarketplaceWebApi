﻿namespace AppsMarketplaceWebApi.DTO
{
	public class LoginDTO
	{
		/// <summary>
		/// The user's name.
		/// </summary>
		public required string Username { get; init; }

		/// <summary>
		/// The user's password.
		/// </summary>
		public required string Password { get; init; }

		/// <summary>
		/// The optional two-factor authenticator code. This may be required for users who have enabled two-factor authentication.
		/// This is not required if a <see cref="TwoFactorRecoveryCode"/> is sent.
		/// </summary>
		public string? TwoFactorCode { get; init; }

		/// <summary>
		/// An optional two-factor recovery code from <see cref="TwoFactorResponse.RecoveryCodes"/>.
		/// This is required for users who have enabled two-factor authentication but lost access to their <see cref="TwoFactorCode"/>.
		/// </summary>
		public string? TwoFactorRecoveryCode { get; init; }
	}
}
