using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.AccessControl.Exceptions;

internal sealed class InvalidApiKeyInputException(string message) : VsaTemplateException(message, 400);
