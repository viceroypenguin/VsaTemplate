using Riok.Mapperly.Abstractions;
using VsaTemplate.Users.Models;

namespace VsaTemplate.Features.Models;

[Mapper]
internal static partial class Mapper
{
	internal static partial IQueryable<Feature> SelectDto(this IQueryable<Database.Models.Feature> q);

	private static FeatureId ToFeatureId(int id) => (FeatureId)id;
	private static UserId ToUserId(int id) => (UserId)id;

	[MapProperty(nameof(Database.Models.Feature.FeatureTypeId), nameof(Database.Models.Feature.FeatureType))]
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

	[MapProperty(nameof(Database.Models.Feature.FeatureType), nameof(Database.Models.Feature.FeatureTypeId))]
	[MapperIgnoreTarget(nameof(Database.Models.Feature.CreatorUser))]
	[MapperIgnoreTarget(nameof(Database.Models.Feature.FeatureId))]
	[MapperIgnoreTarget(nameof(Database.Models.Feature.FeatureType))]
	[MapperIgnoreTarget(nameof(Database.Models.Feature.LastUpdatedTimestamp))]
	internal static partial Database.Models.Feature FromDto(this CreateFeatureDto feature);
}
