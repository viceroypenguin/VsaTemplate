using VsaTemplate.Web.Infrastructure.Exceptions;

namespace VsaTemplate.Web.Features.AccessControl.Exceptions;

public sealed class EmailLoginDeliveryException()
	: VsaTemplateException(
		"The sign-in email could not be sent. Please try again later.",
		StatusCodes.Status503ServiceUnavailable
	);
