using System.Globalization;
using System.Threading.RateLimiting;
using Immediate.Injections.Shared;
using Microsoft.Extensions.Options;

namespace VsaTemplate.Web.Infrastructure.Authentication;

public static class EmailLoginRateLimiting
{
	[RegisterServices]
	public static void AddEmailLoginRateLimiting(IServiceCollection services)
	{
		services.AddRateLimiter(
			options =>
			{
				options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

				options.OnRejected = async (context, _) =>
				{
					var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
						? Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds))
						: 60;

					context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

					await Results.Problem(
							statusCode: StatusCodes.Status429TooManyRequests,
							detail: "Too many sign-in requests. Please try again later."
						)
						.ExecuteAsync(context.HttpContext);
				};

				options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
					context =>
					{
						var path = context.Request.Path.Value?.TrimEnd('/');

						var request = string.Equals(
							path,
							"/api/auth/email/request",
							StringComparison.OrdinalIgnoreCase
						);

						var verify = string.Equals(path, "/api/auth/email/verify", StringComparison.OrdinalIgnoreCase);

						if (!request && !verify)
							return RateLimitPartition.GetNoLimiter("other");

						var loginOptions = context.RequestServices.GetRequiredService<IOptions<EmailLoginOptions>>()
							.Value;

						var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
						var key = (request ? "request:" : "verify:") + ip;
						var limit = request ? loginOptions.RequestsPerMinute : loginOptions.VerificationsPerMinute;

						return RateLimitPartition.GetFixedWindowLimiter(
							key,
							_ => new()
							{
								PermitLimit = limit,
								Window = TimeSpan.FromMinutes(1),
								QueueLimit = 0,
								AutoReplenishment = true,
							}
						);
					}
				);
			}
		);
	}
}
