namespace VsaTemplate.Web.Infrastructure.Jobs;

internal interface IRecurringJob
{
	Task Execute(CancellationToken cancellationToken);
}
