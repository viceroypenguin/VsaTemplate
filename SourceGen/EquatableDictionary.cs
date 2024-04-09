namespace VsaTemplate.SourceGen;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix

public static class EquatableDictionary
{
	public static EquatableDictionary<TValue> ToEquatableDictionary<TValue>(
		this Dictionary<string, TValue>? dict
	) => new(dict);
}

public readonly struct EquatableDictionary<TValue>(
	Dictionary<string, TValue>? dictionary
) : IEquatable<EquatableDictionary<TValue>>
{
	private readonly Dictionary<string, TValue> _dictionary = dictionary ?? [];
	private readonly int _hashCode = BuildHashCode(dictionary ?? []);

	private static int BuildHashCode(Dictionary<string, TValue> dictionary)
	{
		var hashCode = new HashCode();

		foreach (var kvp in dictionary.OrderBy(kvp => kvp.Key))
		{
			hashCode.Add(kvp.Key);
			hashCode.Add(kvp.Value);
		}

		return hashCode.ToHashCode();
	}

	public bool Equals(EquatableDictionary<TValue> other)
		=> ReferenceEquals(_dictionary, other._dictionary)
			|| (GetHashCode() == other.GetHashCode()
				&& _dictionary.Count == other._dictionary.Count
				&& _dictionary
					.All(kvp =>
						other._dictionary.TryGetValue(kvp.Key, out var oValue)
						&& EqualityComparer<TValue>.Default.Equals(kvp.Value, oValue)
					)
				);

	public override bool Equals(object obj) =>
		obj is EquatableDictionary<TValue> dict && Equals(dict);

	public override int GetHashCode() => _hashCode;

	public static bool operator ==(EquatableDictionary<TValue> left, EquatableDictionary<TValue> right) =>
		left.Equals(right);

	public static bool operator !=(EquatableDictionary<TValue> left, EquatableDictionary<TValue> right) =>
		!(left == right);

	public bool TryGetValue(string key, out TValue value) =>
		_dictionary.TryGetValue(key, out value);

	public bool FindValue(string key, out TValue value)
	{
		foreach (var kvp in _dictionary)
		{
			if (key.EndsWith(kvp.Key, StringComparison.Ordinal))
			{
				value = kvp.Value;
				return true;
			}
		}

		value = default!;
		return false;
	}
}
