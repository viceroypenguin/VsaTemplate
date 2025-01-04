using System.Diagnostics.CodeAnalysis;
using VsaTemplate.Api.Infrastructure.Exceptions;

namespace VsaTemplate.Api.Features.Shared.Exceptions;

public sealed class NotFoundException(string entityName)
	: VsaTemplateException($"'{entityName}' could not be found.", 404)
{
	[DoesNotReturn]
	public static void ThrowNotFoundException(string entityName) =>
		throw new NotFoundException(entityName);
}
