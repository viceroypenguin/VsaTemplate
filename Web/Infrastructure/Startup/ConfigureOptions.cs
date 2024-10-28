#pragma warning disable IDE0130 // Namespace does not match folder structure

namespace VsaTemplate;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ConfigureOptionsAttribute : Attribute
{
	/// <summary>
	/// The name of the section from which to configure the options. If not provided, default value is the name of the
	/// class.
	/// </summary>
	public string? SectionName { get; set; }
}
