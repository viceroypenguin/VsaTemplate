using Immediate.Validations.Shared;

namespace VsaTemplate.Web.Infrastructure.Emails;

[Validate]
public sealed partial record EmailMessage : IValidationTarget<EmailMessage>
{
	[NotEmpty]
	[element: NotEmpty]
	public required IReadOnlyList<string> To { get; init; }

	[NotEmpty]
	public required string Subject { get; init; }

	[NotEmpty]
	public required string Body { get; init; }
}
