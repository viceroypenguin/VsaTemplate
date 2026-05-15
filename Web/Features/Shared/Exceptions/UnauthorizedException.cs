using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using HttpStatusExceptions;

namespace VsaTemplate.Web.Features.Shared.Exceptions;

public sealed class UnauthorizedException(string? message = null)
	: HttpStatusException(
		statusCode: (int)HttpStatusCode.Forbidden,
		string.IsNullOrWhiteSpace(message) ? "Unauthorized" : $"Unauthorized ({message})"
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
