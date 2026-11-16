using Immediate.Injections.Shared;
using Immediate.Validations.Shared;

namespace VsaTemplate.Web.Infrastructure.Authentication;

[RegisterOptions(nameof(EmailLoginOptions), ValidateOnStart = true)]
[Validate]
public sealed partial class EmailLoginOptions : IValidationTarget<EmailLoginOptions>
{
	[GreaterThan(0)]
	public int RequestsPerMinute { get; init; } = 10;

	[GreaterThan(0)]
	public int VerificationsPerMinute { get; init; } = 30;
}
