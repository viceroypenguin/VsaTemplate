using CommunityToolkit.Diagnostics;
using Immediate.Injections.Shared;
using Immediate.Validations.Shared;
using Resend;
using VsaTemplate.Web.Utilities.Attributes;

namespace VsaTemplate.Web.Infrastructure.Emails;

[ConfigureOptions]
[Validate]
public sealed partial class EmailServiceOptions : IValidationTarget<EmailServiceOptions>
{
	[NotEmpty]
	public required string ApiToken { get; init; }

	[NotEmpty]
	public required string FromEmailAddress { get; init; }

	public required IReadOnlyList<string> AdminEmailAddresses { get; init; }
}

[RegisterScoped]
public sealed class EmailService(
	ResendClient resendClient,
	EmailServiceOptions options,
	IHostEnvironment environment
)
{
	private readonly EmailServiceOptions _options = options;

	public async Task SendAdminEmail(string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
	{
		Guard.IsNotNull(subject);
		Guard.IsNotNull(body);

		var message = new EmailMessage
		{
			Subject = subject,
		};

		message.SetBody(body, isHtml);

		await SendAdminEmail(message, cancellationToken);
	}

	public async Task SendEmail(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
	{
		Guard.IsNotNull(subject);
		Guard.IsNotNull(body);

		var message = new EmailMessage
		{
			To = EmailAddress.Parse(to),
			Subject = subject,
		};

		message.SetBody(body, isHtml);

		await SendEmail(message, cancellationToken);
	}

	public Task SendAdminEmail(EmailMessage message, CancellationToken cancellationToken = default)
	{
		Guard.IsNotNull(message);

		message.SetAdminTo(_options.AdminEmailAddresses);

		return SendEmail(message, cancellationToken);
	}

	public async Task SendEmail(EmailMessage message, CancellationToken cancellationToken = default)
	{
		Guard.IsNotNull(message);

		message.From = EmailAddress.Parse(_options.FromEmailAddress);

		if (!environment.IsProduction())
			message.SetAdminTo(_options.AdminEmailAddresses);

		await resendClient.EmailSendAsync(message, cancellationToken);
	}

}
file static class Extensions
{
	public static void SetBody(this EmailMessage message, string body, bool isHtml)
	{
		if (isHtml)
			message.HtmlBody = body;
		else
			message.TextBody = body;
	}

	public static void SetAdminTo(this EmailMessage message, IReadOnlyList<string> adminEmailAddresses)
	{
		message.To.Clear();
		foreach (var email in adminEmailAddresses)
			message.To.Add(EmailAddress.Parse(email));
	}
}
