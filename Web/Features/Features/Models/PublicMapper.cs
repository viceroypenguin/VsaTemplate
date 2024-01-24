using Riok.Mapperly.Abstractions;

namespace VsaTemplate.Web.Features.Features.Models;

[Mapper(UseDeepCloning = true)]
public static partial class PublicMapper
{
	public static partial Feature Clone(this Feature feature);

	public static partial void CloneTo(this Feature feature, Feature target);
}
