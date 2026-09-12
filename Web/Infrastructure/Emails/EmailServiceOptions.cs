using Immediate.Validations.Shared;
using VsaTemplate.Web.Utilities.Attributes;

namespace VsaTemplate.Web.Infrastructure.Emails;

public enum EmailServerType
{
	None = 0,
	Smtp,
	Resend,

	Disabled = -1,
}

[ConfigureOptions]
[Validate]
public sealed partial class EmailServiceOptions : IValidationTarget<EmailServiceOptions>
{
	public required EmailServerType ServerType { get; init; }

	[NotEmpty]
	public string? SmtpServer { get; init; }

	[GreaterThan(0)]
	public int? SmtpPort { get; init; }

	[NotEmpty]
	public string? ResendApiToken { get; init; }

	[NotEmpty]
	public required string FromEmailAddress { get; init; }

	public required IReadOnlyList<string> AdminEmailAddresses { get; init; }

	private static void AdditionalValidations(ValidationResult errors, EmailServiceOptions target)
	{
		switch (target.ServerType)
		{
			case EmailServerType.Smtp:
				if (target.SmtpServer is null)
				{
					errors.Add(
						new()
						{
							PropertyName = nameof(SmtpServer),
							ErrorMessage = $"`{nameof(SmtpServer)}` is required when `{nameof(ServerType)}` is `{nameof(ServerType.Smtp)}`",
						}
					);
				}

				if (target.SmtpPort is null)
				{
					errors.Add(
						new()
						{
							PropertyName = nameof(SmtpPort),
							ErrorMessage = $"`{nameof(SmtpPort)}` is required when `{nameof(ServerType)}` is `{nameof(ServerType.Smtp)}`",
						}
					);
				}

				return;

			case EmailServerType.Resend:
				if (target.ResendApiToken is null)
				{
					errors.Add(
						new()
						{
							PropertyName = nameof(ResendApiToken),
							ErrorMessage = $"`{nameof(ResendApiToken)}` is required when `{nameof(ServerType)}` is `{nameof(ServerType.Resend)}`",
						}
					);
				}

				return;

			case EmailServerType.None:
				errors.Add(
					new()
					{
						PropertyName = nameof(ServerType),
						ErrorMessage = $"`{nameof(ServerType)}` has not been provided.",
					}
				);

				return;

			case EmailServerType.Disabled:
			default:
				return;
		}
	}
}
