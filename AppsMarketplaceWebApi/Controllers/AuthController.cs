using AppsMarketplaceDTO;
using AppsMarketplaceWebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text;
using tusdotnet.Stores;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AppsMarketplaceWebApi.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController(UserManager<User> userManager, TimeProvider timeProvider,
		IOptionsMonitor<BearerTokenOptions> bearerTokenOptions, IEmailSender<User> emailSender, 
		LinkGenerator linkGenerator, IConfiguration configuration) : ControllerBase
	{
		// Validate the email address using DataAnnotations like the UserValidator does when RequireUniqueEmail = true.
		private static readonly EmailAddressAttribute _emailAddressAttribute = new();

		protected readonly TimeProvider _timeProvider = timeProvider;
		protected readonly IOptionsMonitor<BearerTokenOptions> _bearerTokenOptions = bearerTokenOptions;
		protected readonly IEmailSender<User> _emailSender = emailSender;
		protected readonly LinkGenerator _linkGenerator = linkGenerator;

		protected readonly IConfiguration _configuration = configuration;

		// We'll figure out a unique endpoint name based on the final route pattern during endpoint generation.
		const string confirmEmailEndpointName = $"{nameof(AuthController)}-ConfirmEmail";

		protected readonly UserManager<User> _userManager = userManager;

		#region AUTHENTICATION AND AUTHORIZATION
		//--------------------------------------AUTHENTICATION AND AUTHORIZATION--------------------------------------//
		[HttpPost("Register")]
		public async Task<Results<Ok, ValidationProblem, ProblemHttpResult, BadRequest<string>>> Register([FromBody] RegistrationDTO registration,
			[FromServices] UserManager<User> userManager, [FromServices] AppDbContext dbContext, [FromServices] IServiceProvider sp)
		{
			if (!userManager.SupportsUserEmail)
			{
				throw new NotSupportedException($"{nameof(AuthController)} requires a user store with email support.");
			}

			string trimmedDispName = registration.DisplayName.Trim();
			if (trimmedDispName.Length == 0 || trimmedDispName.Length > 25)
			{
				return CreateValidationProblem("InvalidDispName", "Invalid display name! The display name must be non zero, <= 25 character length string after trimming");
			}

			var userStore = sp.GetRequiredService<IUserStore<User>>();

			string trimmedUsername = registration.Username.Trim();

			if (string.IsNullOrEmpty(trimmedUsername) || trimmedUsername.Length == 0 || trimmedUsername.Length > 25)
			{
				return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidUserName(trimmedUsername)));
			}

			User? possibleConflict = await userManager.FindByNameAsync(trimmedUsername);
			if (possibleConflict != null)
			{
				return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.DuplicateUserName(trimmedUsername)));
			}

			User user = new();
			user.DisplayName = registration.DisplayName;

			string email = null!;
			if (registration.Email != null)
			{
				email = registration.Email;
				int conflicts = await dbContext.Users.AsNoTracking().Where(u => u.Email == email && u.EmailConfirmed).CountAsync();
				if (conflicts != 0)
				{
					return TypedResults.BadRequest("The email is already taken.");
				}

				var emailStore = (IUserEmailStore<User>)userStore;

				if (string.IsNullOrEmpty(email) || !_emailAddressAttribute.IsValid(email))
				{
					return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidEmail(email)));
				}

				await emailStore.SetEmailAsync(user, email, CancellationToken.None);
			}

			string? pathToDefaultAvatarPic = _configuration.GetSection("DefaultUserAvatarPath").Value;
			if (pathToDefaultAvatarPic == null)
				throw new Exception("Couldn't fine the default user avatar path in the configuration file, make sure you have 'DefaultUserAvatarPath' field set");

			string? pathToImagesDir = _configuration.GetSection("ImageStore").Value;
			if (pathToImagesDir == null)
				throw new Exception("Couldn't fine the path to user avatar directory in the configuration file, make sure you have 'ImageStore' field set");

			string pathToAvatar = pathToImagesDir + $"/{user.Id}.png";

			try
			{
				System.IO.File.Copy(pathToDefaultAvatarPic, pathToAvatar);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				return TypedResults.Problem(statusCode: 500);
			}

			user.PathToAvatarPic = pathToAvatar;

			await userStore.SetUserNameAsync(user, trimmedUsername, CancellationToken.None);
			var result = await userManager.CreateAsync(user, registration.Password);

			if (!result.Succeeded)
			{
				return CreateValidationProblem(result);
			}

			if(registration.Email != null)
			{
				await SendConfirmationEmailAsync(user, userManager, HttpContext, email);
			}

			return TypedResults.Ok();
		}

		[HttpPost("Login")]
		public async Task<Results<Ok<AccessTokenResponse>, EmptyHttpResult, ProblemHttpResult>> 
			Login([FromBody] LoginDTO login, [FromQuery] bool? useCookies, [FromQuery] bool? useSessionCookies, [FromServices] SignInManager<User> signInManager)
		{
			var useCookieScheme = (useCookies == true) || (useSessionCookies == true);
			var isPersistent = (useCookies == true) && (useSessionCookies != true);
			signInManager.AuthenticationScheme = useCookieScheme ? IdentityConstants.ApplicationScheme : IdentityConstants.BearerScheme;

			var result = await signInManager.PasswordSignInAsync(login.Username, login.Password, isPersistent, lockoutOnFailure: true);

			if (result.RequiresTwoFactor)
			{
				if (!string.IsNullOrEmpty(login.TwoFactorCode))
				{
					result = await signInManager.TwoFactorAuthenticatorSignInAsync(login.TwoFactorCode, isPersistent, rememberClient: isPersistent);
				}
				else if (!string.IsNullOrEmpty(login.TwoFactorRecoveryCode))
				{
					result = await signInManager.TwoFactorRecoveryCodeSignInAsync(login.TwoFactorRecoveryCode);
				}
			}

			if (!result.Succeeded)
			{
				return TypedResults.Problem(result.ToString(), statusCode: StatusCodes.Status401Unauthorized);
			}

			// The signInManager already produced the needed response in the form of a cookie or bearer token.
			return TypedResults.Empty;
		}

		[HttpPost("Refresh")]
		public async Task<Results<Ok<AccessTokenResponse>, UnauthorizedHttpResult, SignInHttpResult, ChallengeHttpResult>>
			Refresh([FromBody] RefreshRequest refreshRequest, [FromServices] SignInManager<User> signInManager)
		{
			var refreshTokenProtector = _bearerTokenOptions.Get(IdentityConstants.BearerScheme).RefreshTokenProtector;
			var refreshTicket = refreshTokenProtector.Unprotect(refreshRequest.RefreshToken);

			// Reject the /refresh attempt with a 401 if the token expired or the security stamp validation fails
			if (refreshTicket?.Properties?.ExpiresUtc is not { } expiresUtc ||
				_timeProvider.GetUtcNow() >= expiresUtc ||
				await signInManager.ValidateSecurityStampAsync(refreshTicket.Principal) is not User user)

			{
				return TypedResults.Challenge();
			}

			var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
			return TypedResults.SignIn(newPrincipal, authenticationScheme: IdentityConstants.BearerScheme);
		}

		[Authorize]
		[HttpPatch("ChangeEmail")]
		public async Task<Results<Ok, ValidationProblem, UnauthorizedHttpResult, BadRequest<string>>> ChangeEmail([FromQuery] string newEmail, 
			[FromServices] UserManager<User> userManager, [FromServices] AppDbContext dbContext, [FromServices] IServiceProvider sp)
		{
			User? user = await _userManager.GetUserAsync(User);
			if (user == null)
				return TypedResults.Unauthorized();

			int conflicts = await dbContext.Users.AsNoTracking().Where(u => u.Email == newEmail && u.EmailConfirmed).CountAsync();
			if(conflicts != 0)
			{
				return TypedResults.BadRequest("The email is already taken.");
			}

			if (string.IsNullOrEmpty(newEmail) || !_emailAddressAttribute.IsValid(newEmail))
			{
				return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidEmail(newEmail)));
			}

			var userStore = sp.GetRequiredService<IUserStore<User>>();
			var emailStore = (IUserEmailStore<User>)userStore;

			await emailStore.SetEmailAsync(user, newEmail, CancellationToken.None);

			await SendConfirmationEmailAsync(user, userManager, HttpContext, newEmail, true);

			return TypedResults.Ok();
		}

		[EndpointName(confirmEmailEndpointName)]
		[HttpGet("ConfirmEmail")]
		public async Task<Results<ContentHttpResult, UnauthorizedHttpResult, BadRequest<string>, ValidationProblem>> 
			ConfirmEmail([FromQuery] string userId, [FromQuery] string code, [FromQuery] string? changedEmail, 
			[FromServices] UserManager<User> userManager, [FromServices] AppDbContext dbContext)
		{
			if (await userManager.FindByIdAsync(userId) is not { } user)
			{
				// We could respond with a 404 instead of a 401 like Identity UI, but that feels like unnecessary information.
				return TypedResults.Unauthorized();
			}

			try
			{
				code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
			}
			catch (FormatException)
			{
				return TypedResults.Unauthorized();
			}

			IdentityResult result;

			string? email;

			if (changedEmail != null)
				email = changedEmail;
			else
				email = user.Email;

			if(email != null)
			{
				int conflicts = await dbContext.Users.AsNoTracking().Where(u => u.Email == email && u.EmailConfirmed).CountAsync();
				if (conflicts != 0)
				{
					return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.DuplicateEmail(email)));
				}
			}
			else
			{
				return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidEmail(email)));
			}

			if (string.IsNullOrEmpty(changedEmail))
			{
				result = await userManager.ConfirmEmailAsync(user, code);
			}
			else
			{
				if (!_emailAddressAttribute.IsValid(changedEmail))
				{
					return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidEmail(changedEmail)));
				}

				result = await userManager.ChangeEmailAsync(user, changedEmail, code);
			}

			if (!result.Succeeded)
			{
				return TypedResults.Unauthorized();
			}

			return TypedResults.Text("Thank you for confirming your email.");
		}

		[HttpPost("ResendConfirmationEmail")]
		public async Task<Ok> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest resendRequest,
			HttpContext context, [FromServices] UserManager<User> userManager)
		{
			if (await userManager.FindByEmailAsync(resendRequest.Email) is not { } user)
			{
				return TypedResults.Ok();
			}

			await SendConfirmationEmailAsync(user, userManager, context, resendRequest.Email);
			return TypedResults.Ok();
		}

		[HttpPost("ForgotPassword")]
		public async Task<Results<Ok, ValidationProblem>> ForgotPassword([FromBody] ForgotPasswordRequest resetRequest,
			[FromServices] UserManager<User> userManager, [FromServices] AppDbContext dbContext)
		{
			User? user = await dbContext.Users.Where(u => u.Email == resetRequest.Email && u.EmailConfirmed).FirstOrDefaultAsync();

			if (user != null)
			{
				var code = await userManager.GeneratePasswordResetTokenAsync(user);
				code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

				await _emailSender.SendPasswordResetCodeAsync(user, resetRequest.Email, code);
			}

			// Don't reveal that the user does not exist or is not confirmed, so don't return a 200 if we would have
			// returned a 400 for an invalid code given a valid user email.
			return TypedResults.Ok();
		}

		[HttpPost("ResetPassword")]
		public async Task<Results<Ok, ValidationProblem>> ResetPassword([FromBody] ResetPasswordRequest resetRequest,
			[FromServices] UserManager<User> userManager, [FromServices] AppDbContext dbContext)
		{
			User? user = await dbContext.Users.Where(u => u.Email == resetRequest.Email && u.EmailConfirmed).FirstOrDefaultAsync();

			if (user == null)
			{
				// Don't reveal that the user does not exist or is not confirmed, so don't return a 200 if we would have
				// returned a 400 for an invalid code given a valid user email.
				return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken()));
			}

			IdentityResult result;
			try
			{
				var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(resetRequest.ResetCode));
				result = await userManager.ResetPasswordAsync(user, code, resetRequest.NewPassword);
			}
			catch (FormatException)
			{
				result = IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken());
			}

			if (!result.Succeeded)
			{
				return CreateValidationProblem(result);
			}

			return TypedResults.Ok();
		}

		[Authorize]
		[HttpPost("Manage/2fa")]
		public async Task<Results<Ok<TwoFactorResponse>, ValidationProblem, NotFound>>
			TwoFactor([FromBody] TwoFactorRequest tfaRequest, [FromServices] SignInManager<User> signInManager)
		{
			var userManager = signInManager.UserManager;
			if (await userManager.GetUserAsync(User) is not { } user)
			{
				return TypedResults.NotFound();
			}

			if (tfaRequest.Enable == true)
			{
				if (tfaRequest.ResetSharedKey)
				{
					return CreateValidationProblem("CannotResetSharedKeyAndEnable",
						"Resetting the 2fa shared key must disable 2fa until a 2fa token based on the new shared key is validated.");
				}
				else if (string.IsNullOrEmpty(tfaRequest.TwoFactorCode))
				{
					return CreateValidationProblem("RequiresTwoFactor",
						"No 2fa token was provided by the request. A valid 2fa token is required to enable 2fa.");
				}
				else if (!await userManager.VerifyTwoFactorTokenAsync(user, userManager.Options.Tokens.AuthenticatorTokenProvider, tfaRequest.TwoFactorCode))
				{
					return CreateValidationProblem("InvalidTwoFactorCode",
						"The 2fa token provided by the request was invalid. A valid 2fa token is required to enable 2fa.");
				}

				await userManager.SetTwoFactorEnabledAsync(user, true);
			}
			else if (tfaRequest.Enable == false || tfaRequest.ResetSharedKey)
			{
				await userManager.SetTwoFactorEnabledAsync(user, false);
			}

			if (tfaRequest.ResetSharedKey)
			{
				await userManager.ResetAuthenticatorKeyAsync(user);
			}

			string[]? recoveryCodes = null;
			if (tfaRequest.ResetRecoveryCodes || (tfaRequest.Enable == true && await userManager.CountRecoveryCodesAsync(user) == 0))
			{
				var recoveryCodesEnumerable = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
				recoveryCodes = recoveryCodesEnumerable?.ToArray();
			}

			if (tfaRequest.ForgetMachine)
			{
				await signInManager.ForgetTwoFactorClientAsync();
			}

			var key = await userManager.GetAuthenticatorKeyAsync(user);
			if (string.IsNullOrEmpty(key))
			{
				await userManager.ResetAuthenticatorKeyAsync(user);
				key = await userManager.GetAuthenticatorKeyAsync(user);

				if (string.IsNullOrEmpty(key))
				{
					throw new NotSupportedException("The user manager must produce an authenticator key after reset.");
				}
			}

			return TypedResults.Ok(new TwoFactorResponse
			{
				SharedKey = key,
				RecoveryCodes = recoveryCodes,
				RecoveryCodesLeft = recoveryCodes?.Length ?? await userManager.CountRecoveryCodesAsync(user),
				IsTwoFactorEnabled = await userManager.GetTwoFactorEnabledAsync(user),
				IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user),
			});
		}

		[Authorize]
		[HttpGet("Manage/Info")]
		public async Task<Results<Ok<InfoResponse>, ValidationProblem, NotFound>>
			InfoGet([FromServices] UserManager<User> userManager)
		{
			if (await userManager.GetUserAsync(User) is not { } user)
			{
				return TypedResults.NotFound();
			}

			return TypedResults.Ok(await CreateInfoResponseAsync(user, userManager));
		}

		[Authorize]
		[HttpPost("Manage/Info")]
		public async Task<Results<Ok<InfoResponse>, ValidationProblem, NotFound>>
			InfoPost([FromBody] InfoRequest infoRequest,
			HttpContext context, [FromServices] UserManager<User> userManager)
		{
			if (await userManager.GetUserAsync(User) is not { } user)
			{
				return TypedResults.NotFound();
			}

			if (!string.IsNullOrEmpty(infoRequest.NewEmail) && !_emailAddressAttribute.IsValid(infoRequest.NewEmail))
			{
				return CreateValidationProblem(IdentityResult.Failed(userManager.ErrorDescriber.InvalidEmail(infoRequest.NewEmail)));
			}

			if (!string.IsNullOrEmpty(infoRequest.NewPassword))
			{
				if (string.IsNullOrEmpty(infoRequest.OldPassword))
				{
					return CreateValidationProblem("OldPasswordRequired",
						"The old password is required to set a new password. If the old password is forgotten, use /resetPassword.");
				}

				var changePasswordResult = await userManager.ChangePasswordAsync(user, infoRequest.OldPassword, infoRequest.NewPassword);
				if (!changePasswordResult.Succeeded)
				{
					return CreateValidationProblem(changePasswordResult);
				}
			}

			if (!string.IsNullOrEmpty(infoRequest.NewEmail))
			{
				var email = await userManager.GetEmailAsync(user);

				if (email != infoRequest.NewEmail)
				{
					await SendConfirmationEmailAsync(user, userManager, context, infoRequest.NewEmail, isChange: true);
				}
			}

			return TypedResults.Ok(await CreateInfoResponseAsync(user, userManager));
		}

		async Task SendConfirmationEmailAsync(User user, UserManager<User> userManager, HttpContext context, string email, bool isChange = false)
		{
			var code = isChange
				? await userManager.GenerateChangeEmailTokenAsync(user, email)
				: await userManager.GenerateEmailConfirmationTokenAsync(user);
			code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

			var userId = await userManager.GetUserIdAsync(user);
			var routeValues = new RouteValueDictionary()
			{
				["userId"] = userId,
				["code"] = code,
			};

			if (isChange)
			{
				// This is validated by the /confirmEmail endpoint on change.
				routeValues.Add("changedEmail", email);
			}

			var confirmEmailUrl = _linkGenerator.GetUriByName(context, confirmEmailEndpointName, routeValues)
				?? throw new NotSupportedException($"Could not find endpoint named '{confirmEmailEndpointName}'.");

			//await _emailSender.SendConfirmationLinkAsync(user, email, HtmlEncoder.Default.Encode(confirmEmailUrl));
			await _emailSender.SendConfirmationLinkAsync(user, email, confirmEmailUrl);
		}

		private static ValidationProblem CreateValidationProblem(string errorCode, string errorDescription)
		{
			return TypedResults.ValidationProblem(new Dictionary<string, string[]>
			{
				{ errorCode, [errorDescription] }
			});
		}

		private static ValidationProblem CreateValidationProblem(IdentityResult result)
		{
			// We expect a single error code and description in the normal case.
			// This could be golfed with GroupBy and ToDictionary, but perf! :P
			Debug.Assert(!result.Succeeded);
			var errorDictionary = new Dictionary<string, string[]>(1);

			foreach (var error in result.Errors)
			{
				string[] newDescriptions;

				if (errorDictionary.TryGetValue(error.Code, out var descriptions))
				{
					newDescriptions = new string[descriptions.Length + 1];
					Array.Copy(descriptions, newDescriptions, descriptions.Length);
					newDescriptions[descriptions.Length] = error.Description;
				}
				else
				{
					newDescriptions = [error.Description];
				}

				errorDictionary[error.Code] = newDescriptions;
			}

			return TypedResults.ValidationProblem(errorDictionary);
		}

		private static async Task<InfoResponse> CreateInfoResponseAsync<TUser>(TUser user, UserManager<TUser> userManager)
		where TUser : class
		{
			return new()
			{
				Email = await userManager.GetEmailAsync(user) ?? throw new NotSupportedException("Users must have an email."),
				IsEmailConfirmed = await userManager.IsEmailConfirmedAsync(user),
			};
		}
		//--------------------------------------AUTHENTICATION AND AUTHORIZATION--------------------------------------//
		#endregion
	}
}
