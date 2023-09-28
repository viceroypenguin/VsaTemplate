namespace VsaTemplate.Support;

public interface IRecurringJob
{
	Task Execute(CancellationToken cancellationToken);
}
