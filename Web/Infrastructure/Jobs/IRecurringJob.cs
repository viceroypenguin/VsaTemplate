namespace VsaTemplate.Web.Infrastructure.Jobs;

public interface IRecurringJob
{
	Task Execute(CancellationToken cancellationToken);
}
