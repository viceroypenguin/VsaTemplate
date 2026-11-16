using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.Organizations.Exceptions;

internal sealed class InvalidOrganizationInputException(string message) : VsaTemplateException(message, 400);
