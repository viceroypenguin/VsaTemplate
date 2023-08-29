using VsaTemplate.Users.Models;

namespace VsaTemplate.Features.Models;

public sealed record Feature
{
	public FeatureId FeatureId { get; set; }
	public required string Name { get; set; }
	public FeatureType FeatureType { get; set; }
	public UserId CreatorUserId { get; set; }
	public DateTimeOffset LastUpdatedTimestamp { get; set; }
	public int ValueA { get; set; }
	public string? ValueB { get; set; }

	public string? CreatorName { get; set; }

	public override int GetHashCode() =>
		FeatureId.GetHashCode();

	public bool Equals(Feature? other) =>
		other != null
		&& FeatureId.Equals(other.FeatureId);
}
