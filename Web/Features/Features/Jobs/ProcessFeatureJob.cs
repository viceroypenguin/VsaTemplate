using System.Collections.Concurrent;
using System.Xml.Linq;
using LinqToDB;
using Microsoft.Extensions.Options;
using SuperLinq;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Infrastructure.Emails;
using VsaTemplate.Web.Infrastructure.Jobs;

namespace VsaTemplate.Web.Features.Features.Jobs;

[ConfigureOptions]
public sealed class DownloadAlphaVantageDataJobOptions
{
	public bool Enabled { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage(
	"Performance",
	"CA1848:Use the LoggerMessage delegates",
	Justification = "Logging performance is not critical here.")]
[RecurringJob(
	RecurringJobId = "process-feature-job",
	Cron = "0 0 * * *",
	TimeZone = "Eastern Standard Time")]
[RegisterScoped]
public sealed partial class ProcessFeatureJob(
	DbContext context,
	EmailService emailService,
	ILogger<ProcessFeatureJob> logger,
	IOptions<DownloadAlphaVantageDataJobOptions> options
) : IRecurringJob
{
	private readonly DateOnly _date = DateOnly.FromDateTime(
		TimeZoneInfo.ConvertTime(
			DateTimeOffset.Now,
			TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")).Date
	);

	private readonly ConcurrentDictionary<int, LogEntry> _emailLogs = new();
	private int _logNumber;

	private sealed class LogEntry
	{
		public required string Group { get; set; }
		public required string Message { get; set; }
		public required string ExceptionMessage { get; set; }
		public DateTimeOffset Timestamp { get; internal set; }
	}

	public async Task Execute(CancellationToken cancellationToken)
	{
		if (!options.Value.Enabled) return;

		try
		{
			_ = await context.Features.FirstOrDefaultAsync(cancellationToken);
			await Task.Delay(10_000, cancellationToken);
			await SendEmail();
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			AddEmailLog(DateTimeOffset.Now, "Main", "Process Failed:", ex.Message);
			logger.LogError(ex, "Unable to process feature.");
			throw;
		}
		finally
		{
			await SendEmail();
		}
	}

	private void AddEmailLog(DateTimeOffset timestamp, string group, string message, string exceptionMessage) =>
		_emailLogs[Interlocked.Increment(ref _logNumber)] =
			new LogEntry
			{
				Timestamp = timestamp,
				Group = group,
				Message = message,
				ExceptionMessage = exceptionMessage,
			};

	private async Task SendEmail()
	{
		var entries = _emailLogs
			.GroupBy(le => le.Value.Group, StringComparer.OrdinalIgnoreCase)
			.ToList();

		var groups = entries
			.SelectMany(g => new[]
			{
				new XElement("hr"),
				new XElement("h4", g.Key),
				new XElement("table",
					new XAttribute("style", "width: 100%"),
					new XElement("thead",
						new XElement("tr",
							new XElement("th", "Timestamp"),
							new XElement("th", "Symbol"),
							new XElement("th", "Message"),
							new XElement("th", "Exception"))),
					new XElement("tbody", g
						.OrderBy(kvp => kvp.Key)
						.Select(kvp => kvp.Value)
						.Select(le => new XElement("tr",
							new XElement("td", le.Timestamp),
							new XElement("td", le.Message),
							new XElement("td", le.ExceptionMessage)))
						.ToArray())),
					});

		var succeeded =
			entries.Count is 0 || !entries.Any(g => string.Equals(g.Key, "Main", StringComparison.Ordinal))
			? "Run Succeeded" : "Run Failed";

		var body = new XElement("body",
			new[]
				{
				new XElement("h3", succeeded),
			}.Concat(groups).ToArray());
		var subject = $"[Process Feature Job {_date}] {succeeded}";

		await emailService.SendAdminEmail(subject, body.ToString(), isHtml: true, cancellationToken: default);
	}
}
