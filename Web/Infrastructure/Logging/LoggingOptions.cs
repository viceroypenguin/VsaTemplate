using Immediate.Validations.Shared;
using VsaTemplate.Web.Utilities.Attributes;

namespace VsaTemplate.Web.Infrastructure.Logging;

[ConfigureOptions]
[Validate]
public sealed partial class LoggingOptions : IValidationTarget<LoggingOptions>
{
	public Uri? SeqUrl { get; init; }
}
