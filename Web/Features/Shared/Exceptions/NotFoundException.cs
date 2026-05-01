using System.Diagnostics.CodeAnalysis;
using HttpStatusExceptions;

namespace VsaTemplate.Web.Features.Shared.Exceptions;

public sealed class NotFoundException(string entityName, string? message = null)
	: HttpStatusException(
		statusCode: 404,
		string.IsNullOrWhiteSpace(message)
			? $"Record could not be found (Entity: {entityName})"
			: $"{message} (Entity: {entityName})"
	)
{
	public string EntityName { get; } = entityName;

	[DoesNotReturn]
	public static void ThrowNotFoundException(string entityName, string? message = null) =>
		throw new NotFoundException(entityName);

	[DoesNotReturn]
	public static T ThrowNotFoundException<T>(string entityName, string? message = null) =>
		throw new NotFoundException(entityName);
}
