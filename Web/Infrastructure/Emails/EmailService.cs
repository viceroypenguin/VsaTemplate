using CommunityToolkit.Diagnostics;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace VsaTemplate.Web.Infrastructure.Emails;

[ConfigureOptions]
public sealed class EmailServiceOptions
{
	public required string Host { get; set; }
	public required int Port { get; set; }
	public required string Password { get; set; }
	public required string Username { get; set; }
	public required string FromEmailAddress { get; set; }
	public required IReadOnlyList<string> AdminEmailAddresses { get; set; }
}

[RegisterScoped]
public class EmailService
{
	private readonly EmailServiceOptions _options;

	public EmailService(
		IOptionsSnapshot<EmailServiceOptions> options)
	{
		Guard.IsNotNull(options);
		Guard.IsNotNull(options.Value);

		_options = options.Value;
	}

	public async Task SendAdminEmail(string subject, string body, bool isHtml, CancellationToken cancellationToken = default)
	{
		Guard.IsNotNull(subject);
		Guard.IsNotNull(body);

		using var message = new MimeMessage
		{
			Subject = subject,
			Body = new TextPart(isHtml ? "html" : "plain") { Text = body, },
		};

		await SendAdminEmail(message, cancellationToken);
	}

	public Task SendAdminEmail(MimeMessage message, CancellationToken cancellationToken = default)
	{
		Guard.IsNotNull(message);

		message.To.Clear();
		foreach (var email in _options.AdminEmailAddresses)
			message.To.Add(MailboxAddress.Parse(email));

		return SendEmail(message, cancellationToken);
	}

	public async Task SendEmail(MimeMessage message, CancellationToken cancellationToken = default)
	{
		Guard.IsNotNull(message);

		message.From.Clear();
		message.From.Add(MailboxAddress.Parse(_options.FromEmailAddress));

		using var client = new SmtpClient();
		await client.ConnectAsync(_options.Host, _options.Port, cancellationToken: cancellationToken);

		await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
		await client.SendAsync(message);

		await client.DisconnectAsync(quit: true, cancellationToken: cancellationToken);
	}
}
