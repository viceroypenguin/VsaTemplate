using Immediate.Validations.Shared;

namespace VsaTemplate.Web.Features.Users.Models;

[ValueObject<string>]
public readonly partial struct Auth0UserId;

[ValueObject]
[Validate]
public readonly partial struct UserId : IValidationTarget<UserId>
{
	private static void AdditionalValidations(ValidationResult errors, UserId userId)
	{
		errors.Add(
			() => GreaterThanAttribute.ValidateProperty(userId.Value, 0),
			"Id must be greater than zero."
		);
	}
}
