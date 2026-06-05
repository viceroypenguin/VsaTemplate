namespace VsaTemplate.Web.Infrastructure.Exceptions;

public abstract class VsaTemplateException(
	string message,
	int statusCode
) : Exception(message)
{
	public int StatusCode { get; } = statusCode;
}
