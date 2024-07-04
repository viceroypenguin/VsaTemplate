using Immediate.Validations.Shared;

namespace VsaTemplate.Web.Features.Users.Models;

[ValueObject<string>]
[Validate]
public readonly partial struct Auth0UserId : IValidationTarget<Auth0UserId>
{
	private static void AdditionalValidations(ValidationResult errors, Auth0UserId target)
	{
		errors.Add(
			() => NotNullAttribute.ValidateProperty(target.Value),
			"Auth0 Id must not be null."
		);
	}
}

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
