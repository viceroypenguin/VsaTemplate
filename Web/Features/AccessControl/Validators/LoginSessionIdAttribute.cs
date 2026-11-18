using Immediate.Validations.Shared;

namespace VsaTemplate.Web.Features.AccessControl.Validators;

public sealed class LoginSessionIdAttribute : ValidatorAttribute
{
	public static string DefaultMessage => "'{PropertyName}' must be a nonempty UUID in D format.";

	public static bool ValidateProperty(string target) =>
		Guid.TryParseExact(target, "D", out var session) && session != Guid.Empty;
}
