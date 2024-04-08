using System.Diagnostics;
using Immediate.Handlers.Shared;

namespace VsaTemplate.Web.Infrastructure.Behaviors;

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
			["RequestData"] = request,
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
				LogSuccess(HandlerType, sw.Elapsed.TotalMilliseconds);

			return response;
		}
		catch (Exception ex)
		{
			using (var scope = _logger.BeginScope(properties))
				LogException(HandlerType, ex);
			throw;
		}
	}

	[LoggerMessage(
		Level = LogLevel.Information,
		Message = "Executed {Type} handler in {Elapsed} ms"
	)]
	private partial void LogSuccess(
		Type type,
		double elapsed);

	[LoggerMessage(
		Level = LogLevel.Error,
		Message = "Exception during {Type} handler"
	)]
	private partial void LogException(
		Type type,
		Exception exception);
}
