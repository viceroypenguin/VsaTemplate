using System.Diagnostics.CodeAnalysis;
using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.Shared.Exceptions;

public sealed class NotFoundException(string entityName)
	: VsaTemplateException($"'{entityName}' could not be found.", 404)
{
	[DoesNotReturn]
	public static void ThrowNotFoundException(string entityName) =>
		throw new NotFoundException(entityName);
}
