using Immediate.Injections.Shared;

namespace VsaTemplate.Web.Infrastructure.Middleware;

[RegisterSingleton]
public sealed class AddRequestIdHeaderMiddleware : IMiddleware
{
	public Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		context.Response.Headers.Append("RequestId", context.TraceIdentifier);
		return next(context);
	}
}
