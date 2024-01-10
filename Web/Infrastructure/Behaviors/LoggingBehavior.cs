using System.Diagnostics;
using Immediate.Handlers.Shared;

namespace VsaTemplate.Web.Infrastructure.Behaviors;

public partial class LoggingBehavior<TRequest, TResponse>(
	ILogger<LoggingBehavior<TRequest, TResponse>> logger,
	IHttpContextAccessor httpContextAccessor
) : Behavior<TRequest, TResponse>
{
	private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;
	private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

	public override async Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var sw = Stopwatch.StartNew();

		var response = await Next(request, cancellationToken);
		var elapsed = sw.Elapsed;

		if (_httpContextAccessor.HttpContext is not { Request.Path: var path, })
			path = "";

		LogRequest(path, elapsed.TotalMilliseconds, request);

		return response;
	}

	[LoggerMessage(
		Level = LogLevel.Information,
		Message = "Handled request. (Url: {Path}; Time: {TotalMilliseconds} ms; Request: {@Request})")]
	private partial void LogRequest(
		PathString path,
		double totalMilliseconds,
		TRequest request);
}
