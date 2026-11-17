using System.Net.Mail;
using Immediate.Validations.Shared;

namespace VsaTemplate.Web.Features.Shared.Validators;

public sealed class BareEmailAddressAttribute : ValidatorAttribute
{
	public static bool ValidateProperty(string target)
	{
		var email = target?.Trim();

		return MailAddress.TryCreate(email, out var address)
			&& string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);
	}

	public static string DefaultMessage => "'{PropertyName}' must be a valid email address without a display name.";
}
