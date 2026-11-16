namespace VsaTemplate.Web.Infrastructure.Emails;

public interface IEmailSender
{
	Task SendEmail(EmailMessage message, CancellationToken cancellationToken = default);
	Task SendLoginEmail(EmailMessage message, CancellationToken cancellationToken = default);
}
