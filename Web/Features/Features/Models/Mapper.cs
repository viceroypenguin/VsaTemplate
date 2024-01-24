using Riok.Mapperly.Abstractions;
using VsaTemplate.Web.Features.Users.Models;

namespace VsaTemplate.Web.Features.Features.Models;

[Mapper]
internal static partial class Mapper
{
	internal static partial IQueryable<Feature> SelectDto(this IQueryable<Database.Models.Feature> q);

	private static FeatureId ToFeatureId(int id) => (FeatureId)id;
	private static UserId ToUserId(int id) => (UserId)id;

	[MapProperty(nameof(Database.Models.Feature.FeatureTypeId), nameof(Feature.FeatureType))]
	[MapperIgnoreTarget(nameof(Feature.CreatorName))]
	[MapperIgnoreSource(nameof(Database.Models.Feature.CreatorUser))]
	[MapperIgnoreSource(nameof(Database.Models.Feature.FeatureType))]
	internal static partial Feature ToDto(this Database.Models.Feature feature);

	private static int FromFeatureId(FeatureId id) => (int)id;
	private static int FromUserId(UserId id) => (int)id;

	[MapProperty(nameof(Database.Models.Feature.FeatureType), nameof(Database.Models.Feature.FeatureTypeId))]
	[MapperIgnoreSource(nameof(Feature.CreatorName))]
	[MapperIgnoreTarget(nameof(Database.Models.Feature.CreatorUser))]
	[MapperIgnoreTarget(nameof(Database.Models.Feature.FeatureType))]
	internal static partial Database.Models.Feature FromDto(this Feature feature);
}
