namespace VsaTemplate.Web.Utilities.Attributes;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class AlternativeColumnNamesAttribute(params string[] names) : Attribute
{
	public string[] Names { get; } = names;
}
