using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.Tenants.Exceptions;

internal sealed class TenantValidationException(string message)
	: VsaTemplateException(message, StatusCodes.Status400BadRequest);
