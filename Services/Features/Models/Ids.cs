namespace VsaTemplate.Features.Models;

[ValueObject]
public readonly partial struct FeatureId { }

[SyncEnum]
public enum FeatureType
{
	None = 0,
	Simple = 1,
	Complex = 2,
}
