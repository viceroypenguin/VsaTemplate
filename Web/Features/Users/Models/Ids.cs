namespace VsaTemplate.Web.Features.Users.Models;

[ValueObject<string>]
public readonly partial struct Auth0UserId;

[ValueObject]
public readonly partial struct UserId
{
	public static Validation Validate(int value) =>
		value > 0 ? Validation.Ok : Validation.Invalid("Must be greater than zero.");
}
