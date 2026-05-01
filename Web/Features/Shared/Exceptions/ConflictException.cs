using System.Diagnostics.CodeAnalysis;
using HttpStatusExceptions;

namespace VsaTemplate.Web.Features.Shared.Exceptions;

public sealed class ConflictException(string message)
	: HttpStatusException(statusCode: 409, message)
{
	[DoesNotReturn]
	public static void ThrowConflictException(string message) =>
		throw new ConflictException(message);

	[DoesNotReturn]
	public static T ThrowConflictException<T>(string message) =>
		throw new ConflictException(message);
}
