using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Identity;

namespace AppsMarketplaceWebApi
{
	public class EmailSender : IEmailSender
	{
		private readonly ILogger _logger;
		private readonly IConfiguration _configuration;
		private readonly string _password;
		private readonly string _fromAddr;

		public EmailSender(ILogger<EmailSender> logger, IConfiguration configuration)
		{
			_logger = logger;
			_configuration = configuration;

			string? password = _configuration.GetSection("EmailPassword").Value;
			if (string.IsNullOrEmpty(password))
			{
				throw new Exception("Email password not set up in configuration file! Make sure you have a 'EmailPassword' field in the configuration json.");
			}

			_password = password;

			string? fromAddr = _configuration.GetSection("FromEmailAdress").Value;
			if (string.IsNullOrEmpty(fromAddr))
			{
				throw new Exception("Email adress to send emails from is not set up in configuration file! Make sure you have a 'FromEmailAdress' field in the configuration json.");
			}

			_fromAddr = fromAddr;
		}

		public async Task SendEmailAsync(string toEmail, string subject, string message)
		{
			string? password = _configuration.GetSection("EmailPassword").Value;
			if (string.IsNullOrEmpty(password))
			{
				throw new Exception("Email password not set up in configuration file! Make sure you have a 'EmailPassword' field in the configuration json.");
			}

			await Execute(subject, message, toEmail);
		}

		public async Task Execute(string subject, string message, string toEmail)
		{
			MailAddress fromAddr = new(_fromAddr, "From AppMarketplace");
			MailAddress toAddr = new(toEmail);

			SmtpClient smtpClient = new()
			{
				Host = "smtp.gmail.com",
				Port = 587,
				EnableSsl = true,
				DeliveryMethod = SmtpDeliveryMethod.Network,
				UseDefaultCredentials = false,
				Credentials = new NetworkCredential(fromAddr.Address, _password)
			};

			MailMessage mailMsg = new(fromAddr, toAddr)
			{
				Subject = subject,
				Body = message
			};
			using (mailMsg)
			{
				await smtpClient.SendMailAsync(mailMsg);
			}
		}
	}

	public class MessageEmailSender<TUser>(IEmailSender emailSender) : IEmailSender<TUser> where TUser : class
	{
		internal bool IsNoOp => emailSender is NoOpEmailSender;

		public Task SendConfirmationLinkAsync(TUser user, string email, string confirmationLink)
		{
			return emailSender.SendEmailAsync(email, "Confirm your email", 
				$"Please confirm your account by following the link, if you didn't request the link please ignore it and DO NOT follow it: {confirmationLink}");
		}

		public Task SendPasswordResetCodeAsync(TUser user, string email, string resetCode)
		{
			return emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");
		}

		public Task SendPasswordResetLinkAsync(TUser user, string email, string resetLink)
		{
			return emailSender.SendEmailAsync(email, "Reset your password", $"Please reset your password by following the link: {resetLink}");
		}
	}
}
