namespace VsaTemplate.Web.Infrastructure.Jobs;

[AttributeUsage(AttributeTargets.Class)]
public sealed class RecurringJobAttribute : Attribute
{
	/// <summary>
	/// The identifier of the RecurringJob
	/// </summary>
	public required string RecurringJobId { get; set; }

	/// <summary>
	/// A Cron expression for when the job should run
	/// </summary>
	public required string Cron { get; set; }

	/// <summary>
	/// The TimeZone in which to interpret <see cref="Cron"/>. This value will be passed to <see
	/// cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/>. The default value is <c>Utc</c>.
	/// </summary>
	public string TimeZone { get; set; } = "Utc";

	/// <summary>
	/// The queue on which to run this job. The default value is <c>default</c>.
	/// </summary>
	public string Queue { get; set; } = "default";
}
