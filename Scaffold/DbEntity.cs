#pragma warning disable CA1812

using VsaTemplate.SourceGen;

namespace VsaTemplate.Scaffold;

internal sealed record DbEntity
{
	public required string PropertyName { get; init; }
	public required string TypeName { get; init; }
	public required string TableName { get; init; }
	public required string? SchemaName { get; init; }
	public required EquatableReadOnlyList<Property> Properties { get; init; }
	public required EquatableReadOnlyList<Association> Associations { get; init; }
}

internal sealed record Property
{
	public required string TypeName { get; init; }
	public required string PropertyName { get; init; }
	public required string ColumnName { get; init; }
	public required string? DataType { get; init; }
	public required bool ForceNotNull { get; init; }
	public required bool IsPrimaryKey { get; init; }
	public required int? PrimaryKeyOrder { get; init; }
	public required bool IsIdentity { get; init; }
	public required bool SkipOnInsert { get; init; }
	public required bool SkipOnUpdate { get; init; }
}

internal sealed record Association
{
	public required string Name { get; init; }
	public required bool CanBeNull { get; init; }
	public required string ThisKeys { get; init; }
	public required string OtherName { get; init; }
	public required string OtherType { get; init; }
	public required string OtherKeys { get; init; }
}
