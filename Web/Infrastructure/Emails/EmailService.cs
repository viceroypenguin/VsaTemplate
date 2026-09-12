using System.Diagnostics;
using Immediate.Injections.Shared;
using Immediate.Validations.Shared;
using MailKit.Net.Smtp;
using Resend;

namespace VsaTemplate.Web.Infrastructure.Emails;

[RegisterScoped]
public sealed class EmailService(
	ResendClient resendClient,
	EmailServiceOptions options,
	IHostEnvironment environment
)
{
	private readonly EmailServiceOptions _options = options;

	public async Task SendAdminEmail(EmailMessage message, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(message);

		await SendEmail(message with { To = _options.AdminEmailAddresses }, cancellationToken);
	}

	public async Task SendEmail(EmailMessage message, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(message);
		ValidationException.ThrowIfInvalid(message);

		if (!environment.IsProduction())
			message = message with { To = _options.AdminEmailAddresses };

		switch (_options.ServerType)
		{
			case EmailServerType.Smtp:
			{
				using var client = new SmtpClient();

				await client.ConnectAsync(
					_options.SmtpServer ?? throw new UnreachableException("Should have been caught before this."),
					_options.SmtpPort ?? throw new UnreachableException("Should have been caught before this."),
					useSsl: false,
					cancellationToken
				);

				using var mimeMessage = new MimeKit.MimeMessage()
				{
					From = { MimeKit.MailboxAddress.Parse(_options.FromEmailAddress) },
					To = { message.To },
					Subject = message.Subject,
					Body = new MimeKit.TextPart("plain") { Text = message.Body },
				};

				await client.SendAsync(mimeMessage, cancellationToken);
				await client.DisconnectAsync(quit: true, cancellationToken);

				return;
			}

			case EmailServerType.Resend:
			{
				await resendClient.EmailSendAsync(
					new()
					{
						From = _options.FromEmailAddress,
						To = { message.To },
						Subject = message.Subject,
						TextBody = message.Body,
					},
					cancellationToken
				);

				return;
			}

			case EmailServerType.None:
				throw new UnreachableException("Should have been caught before this.");

			case EmailServerType.Disabled:
			default:
				return;
		}
	}
}

file static class Extensions
{
	public static void Add(this List<EmailAddress> addresses, IReadOnlyList<string> moreAddresses)
	{
		foreach (var a in moreAddresses)
			addresses.Add(a);
	}

	public static void Add(this MimeKit.InternetAddressList addresses, IReadOnlyList<string> moreAddresses)
	{
		foreach (var a in moreAddresses)
			addresses.Add(MimeKit.MailboxAddress.Parse(a));
	}
}
