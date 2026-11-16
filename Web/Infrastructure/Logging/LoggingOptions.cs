using Immediate.Injections.Shared;
using Immediate.Validations.Shared;

namespace VsaTemplate.Web.Infrastructure.Logging;

[RegisterOptions(nameof(LoggingOptions), ValidateOnStart = true)]
[Validate]
public sealed partial class LoggingOptions : IValidationTarget<LoggingOptions>
{
	public Uri? SeqUrl { get; init; }
}
