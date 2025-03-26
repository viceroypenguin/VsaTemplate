namespace VsaTemplate.Api.Infrastructure.Jobs;

public interface IRecurringJob
{
	Task Execute(CancellationToken cancellationToken);
}
