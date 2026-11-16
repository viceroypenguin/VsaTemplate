using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.AccessControl.Exceptions;

public sealed class EmailLoginRateLimitException(int retryAfterSeconds)
	: VsaTemplateException("Please wait before requesting another sign-in code.", StatusCodes.Status429TooManyRequests)
{
	public int RetryAfterSeconds { get; } = retryAfterSeconds;
}
