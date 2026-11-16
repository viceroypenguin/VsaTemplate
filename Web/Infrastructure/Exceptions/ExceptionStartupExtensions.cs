using Immediate.Validations.Shared;
using System.Globalization;
using VsaTemplate.Web.Features.AccessControl.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace VsaTemplate.Web.Infrastructure.Exceptions;

public static class ExceptionStartupExtensions
{
	public static void ConfigureProblemDetails(ProblemDetailsOptions options) =>
		options.CustomizeProblemDetails = c =>
		{

			if (c.Exception is null)
				return;

			c.ProblemDetails = c.Exception switch
			{
				BadHttpRequestException ex => new()
				{
					Detail = "The request body or parameters are invalid.",
					Status = ex.StatusCode,
				},

				ValidationException ex => new ValidationProblemDetails(
					ex
						.Errors
						.GroupBy(x => x.PropertyName, StringComparer.OrdinalIgnoreCase)
						.ToDictionary(
							x => x.Key,
							x => x
								.Select(x => x.ErrorMessage)
								.ToArray(),
							StringComparer.OrdinalIgnoreCase
						)
				)
				{
					Status = StatusCodes.Status400BadRequest,
				},

				VsaTemplateException ex => new()
				{
					Detail = ex.Message,
					Status = ex.StatusCode,
				},

				UnauthorizedAccessException ex => new()
				{
					Detail = "Access denied.",
					Status = StatusCodes.Status403Forbidden,
				},

				var ex => new ProblemDetails
				{
					Detail = "An error has occurred.",
					Status = StatusCodes.Status500InternalServerError,
				},
			};

			if (c.Exception is EmailLoginRateLimitException rateLimit)
				c.HttpContext.Response.Headers.RetryAfter = rateLimit.RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);

			c.HttpContext.Response.Headers
				.Append("RequestId", c.HttpContext.TraceIdentifier);

			c.HttpContext.Response.StatusCode =
				c.ProblemDetails.Status
				?? StatusCodes.Status500InternalServerError;

		};

}
