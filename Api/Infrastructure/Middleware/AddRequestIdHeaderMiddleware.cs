namespace VsaTemplate.Api.Infrastructure.Middleware;

[RegisterSingleton(Registration = RegistrationStrategy.Self)]
public sealed class AddRequestIdHeaderMiddleware : IMiddleware
{
	public Task InvokeAsync(HttpContext context, RequestDelegate next)
	{
		context.Response.Headers.Append("RequestId", context.TraceIdentifier);
		return next(context);
	}
}
