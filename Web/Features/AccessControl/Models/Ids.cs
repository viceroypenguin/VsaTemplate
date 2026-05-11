using Immediate.Validations.Shared;
using VsaTemplate.Web.Utilities.Attributes;

namespace VsaTemplate.Web.Features.AccessControl.Models;

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
[AlternativeColumnNames("ApiKeyId")]
public readonly partial struct UserId;

[ValueObject]
public readonly partial struct RoleId;
