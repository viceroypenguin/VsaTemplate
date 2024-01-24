using System.Collections;

namespace VsaTemplate.SourceGen;

/// <summary>
/// An immutable, equatable array. This is equivalent to <see cref="Array"/> but with value equality support.
/// </summary>
/// <typeparam name="T">The type of values in the array.</typeparam>
public readonly struct EquatableReadOnlyList<T>(
	IReadOnlyList<T> collection
) : IEquatable<EquatableReadOnlyList<T>>, IReadOnlyList<T>
	where T : IEquatable<T>
{
	public bool Equals(EquatableReadOnlyList<T> other)
		=> this.SequenceEqual(other);

	public override bool Equals(object? obj)
		=> obj is EquatableReadOnlyList<T> other && Equals(other);

	public override int GetHashCode()
	{
		var hashCode = new HashCode();

		foreach (var item in collection)
			hashCode.Add(item);

		return hashCode.ToHashCode();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
		=> collection.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
		=> collection.GetEnumerator();

	public int Count => collection.Count;
	public T this[int index] => collection[index];

	public static bool operator ==(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
		=> left.Equals(right);

	public static bool operator !=(EquatableReadOnlyList<T> left, EquatableReadOnlyList<T> right)
		=> !left.Equals(right);
}
