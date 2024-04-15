namespace VsaTemplate;

[AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
public sealed class SyncEnumAttribute : Attribute
{
	/// <summary>
	/// If specified, which table the enum should be sync'd to. By default, it will sync to the table with the same name
	/// as the Enum.
	/// </summary>
	public string? Table { get; set; }

	/// <summary>
	/// If <see langword="true"/>, values not specified in the enum will be deleted. Otherwise, sync will only insert
	/// and update values.
	/// </summary>
	public bool DeleteUnknownValues { get; set; } = true;
}
