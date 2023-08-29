using VsaTemplate.Users.Models;

namespace VsaTemplate.Features.Models;

public sealed record CreateFeatureDto
{
	public required string Name { get; init; }
	public required FeatureType FeatureType { get; init; }
	public required UserId CreatorUserId { get; init; }
	public required int ValueA { get; init; }
	public required string? ValueB { get; init; }
}
