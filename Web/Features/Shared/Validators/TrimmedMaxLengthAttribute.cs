using Immediate.Validations.Shared;

namespace VsaTemplate.Web.Features.Shared.Validators;

public sealed class TrimmedMaxLengthAttribute(int maxLength) : ValidatorAttribute
{
	public int MaxLength { get; } = maxLength;

	public static bool ValidateProperty(string target, int maxLength) =>
		target is not null && target.Trim().Length <= maxLength;

	public static string DefaultMessage => "'{PropertyName}' must be at most {MaxLengthValue} characters after trimming.";
}
