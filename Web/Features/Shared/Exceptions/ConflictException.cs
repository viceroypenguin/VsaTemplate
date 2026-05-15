using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HttpStatusExceptions;

namespace VsaTemplate.Web.Features.Shared.Exceptions;

public sealed class ConflictException(string message)
	: HttpStatusException(statusCode: 409, message)
{
	[DoesNotReturn]
	[StackTraceHidden]
	public static void ThrowConflictException(string message) =>
		throw new ConflictException(message);

	[DoesNotReturn]
	[StackTraceHidden]
	public static T ThrowConflictException<T>(string message) =>
		throw new ConflictException(message);
}
