namespace VsaTemplate.Web.Infrastructure.Blazor;

/// <summary>
/// Indicates that the associated property should have a value injected from the
/// scoped service provider during initialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
internal sealed class InjectScopedAttribute : Attribute
{
}
