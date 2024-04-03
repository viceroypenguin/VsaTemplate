namespace VsaTemplate;

public sealed record DbEntity
{
	public required string PropertyName { get; init; }
	public required string TypeName { get; init; }
	public required string TableName { get; init; }
	public required IReadOnlyList<Property> Properties { get; init; }
}

public sealed record Property
{
	public required string TypeName { get; init; }
	public required string PropertyName { get; init; }
	public required string? DataType { get; init; }
	public required bool? CanBeNull { get; init; }
	public required bool IsPrimaryKey { get; init; }
	public required int? PrimaryKeyOrder { get; init; }
	public required bool IsIdentity { get; init; }
	public required bool SkipOnInsert { get; init; }
	public required bool SkipOnUpdate { get; init; }
}
