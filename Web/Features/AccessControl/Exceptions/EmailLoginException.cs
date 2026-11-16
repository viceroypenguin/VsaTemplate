using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.AccessControl.Exceptions;

public sealed class EmailLoginException()
	: VsaTemplateException("The email address, session, or code is invalid, or the code has expired.", StatusCodes.Status400BadRequest);
