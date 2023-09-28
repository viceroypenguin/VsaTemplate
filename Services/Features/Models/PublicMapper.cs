using Riok.Mapperly.Abstractions;

namespace VsaTemplate.Features.Models;

[Mapper(UseDeepCloning = true)]
public static partial class PublicMapper
{
	public static partial Feature Clone(this Feature feature);

	public static partial void CloneTo(this Feature feature, Feature target);
}
