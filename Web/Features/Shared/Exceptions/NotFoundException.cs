using System.Diagnostics.CodeAnalysis;
using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.Shared.Exceptions;

public sealed class NotFoundException(string entityName, string? message = null)
	: VsaTemplateException(
		string.IsNullOrWhiteSpace(message)
			? $"Record could not be found (Entity: {entityName})"
			: $"{message} (Entity: {entityName})",
		statusCode: 404
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
