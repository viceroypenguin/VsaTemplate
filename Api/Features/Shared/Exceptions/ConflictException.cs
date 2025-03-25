using System.Diagnostics.CodeAnalysis;
using VsaTemplate.Api.Infrastructure.Exceptions;

namespace VsaTemplate.Api.Features.Shared.Exceptions;

public sealed class ConflictException(string message)
	: VsaTemplateException(message, statusCode: 409)
{
	[DoesNotReturn]
	public static void ThrowConflictException(string message) =>
		throw new ConflictException(message);

	[DoesNotReturn]
	public static T ThrowConflictException<T>(string message) =>
		throw new ConflictException(message);
}
