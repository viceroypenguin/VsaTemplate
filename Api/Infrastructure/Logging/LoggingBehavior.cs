using System.Diagnostics;
using Immediate.Handlers.Shared;

namespace VsaTemplate.Api.Infrastructure.Logging;

public sealed partial class LoggingBehavior<TRequest, TResponse>(
	ILogger<LoggingBehavior<TRequest, TResponse>> logger,
	IHttpContextAccessor httpContextAccessor
) : Behavior<TRequest, TResponse>
{
	private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;
	private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

	public override async ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken)
	{
		var properties = new Dictionary<string, object?>(StringComparer.Ordinal)
		{
			["@RequestData"] = request,
		};

		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext is not null)
		{
			properties["User"] = httpContext.User?.Identity?.Name;
			properties["RemoteIP"] = httpContext.Connection.RemoteIpAddress;

			var httpRequest = httpContext.Request;
			properties["ConnectingIP"] = httpRequest.Headers["CF-Connecting-IP"];
			properties["RequestMethod"] = httpRequest.Method;
			properties["RequestPath"] = httpRequest.Path.ToString();
		}

		try
		{
			var sw = Stopwatch.StartNew();
			var response = await Next(request, cancellationToken);
			using (var scope = _logger.BeginScope(properties))
				_logger.LogInformation($"Executed {HandlerType.Named("Type")} handler in {sw.Elapsed.TotalMilliseconds.Named("Elapsed")} ms");

			return response;
		}
		catch (Exception ex)
		{
			using (var scope = _logger.BeginScope(properties))
				_logger.LogError(ex, $"Exception during {HandlerType.Named("Type")} handler");

			throw;
		}
	}
}
