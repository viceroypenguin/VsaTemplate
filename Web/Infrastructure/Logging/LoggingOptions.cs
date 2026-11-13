using Immediate.Injections.Shared;
using Immediate.Validations.Shared;
using VsaTemplate.Web.Database;

namespace VsaTemplate.Web.Infrastructure.Logging;

[RegisterOptions(nameof(DbContextOptions), ValidateOnStart = true)]
[Validate]
public sealed partial class LoggingOptions : IValidationTarget<LoggingOptions>
{
	public Uri? SeqUrl { get; init; }
}
