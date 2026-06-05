using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.Shared.Exceptions;

public sealed class UnauthorizedException(string? message = null)
	: VsaTemplateException(
		string.IsNullOrWhiteSpace(message) ? "Unauthorized" : $"Unauthorized ({message})",
		statusCode: (int)HttpStatusCode.Forbidden
	)
{
	[DoesNotReturn]
	[StackTraceHidden]
	public static void ThrowUnauthorizedException(string? message = null) =>
		throw new UnauthorizedException(message);

	[DoesNotReturn]
	[StackTraceHidden]
	public static T ThrowUnauthorizedException<T>(string? message = null) =>
		throw new UnauthorizedException(message);
}
