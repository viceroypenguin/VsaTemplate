namespace VsaTemplate.Web.Infrastructure.Logging;

[ConfigureOptions]
public sealed class LoggingOptions
{
	public required Uri? SeqUrl { get; init; }
}
