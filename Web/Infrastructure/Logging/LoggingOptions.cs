using Immediate.Validations.Shared;

namespace VsaTemplate.Web.Infrastructure.Logging;

[ConfigureOptions]
[Validate]
public sealed partial class LoggingOptions : IValidationTarget<LoggingOptions>
{
	public Uri? SeqUrl { get; init; }
}
