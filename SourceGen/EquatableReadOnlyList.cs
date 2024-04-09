using System.Text.Json.Serialization;

namespace VsaTemplate.SourceGen;

public static class EquatableReadOnlyList
{
	public static EquatableReadOnlyList<T> ToEquatableReadOnlyList<T>(this IEnumerable<T> enumerable)
		=> new(enumerable as IReadOnlyList<T> ?? enumerable.ToList());
}

/// <summary>
///     A wrapper for IReadOnlyList that provides value equality support for the wrapped list.
/// </summary>
[method: JsonConstructor]
public readonly struct EquatableReadOnlyList<T>(
	IReadOnlyList<T>? collection
) : IEquatable<EquatableReadOnlyList<T>>
{
	public IReadOnlyList<T> Collection => collection ?? [];

	public bool Equals(EquatableReadOnlyList<T> other)
		=> this.Collection.SequenceEqual(other.Collection ?? []);

	public override bool Equals(object? obj)
		=> obj is EquatableReadOnlyList<T> other && Equals(other);

	public override int GetHashCode()
	{
		var hashCode = new HashCode();

		foreach (var item in Collection)
			hashCode.Add(item);

		return hashCode.ToHashCode();
	}

	public int Count => Collection.Count;
	public T this[int index] => Collection[index];

	public static bool operator ==(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
		=> left.Equals(right);

	public static bool operator !=(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
		=> !left.Equals(right);
}
