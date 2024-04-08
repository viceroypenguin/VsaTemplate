namespace VsaTemplate.Web.Features.Features.Models;

internal static class Mapper
{
	internal static IQueryable<Feature> SelectDto(this IQueryable<Database.Models.Feature> q) => q
		.Select(f => new Feature()
		{
			FeatureId = f.FeatureId,
			FeatureType = f.FeatureTypeId,
			Name = f.Name,
			ValueA = f.ValueA,
			ValueB = f.ValueB,
			CreatorUserId = f.CreatorUserId,
			LastUpdatedTimestamp = f.LastUpdatedTimestamp,

			CreatorName = f.CreatorUser.Name,
		});

	internal static Feature ToDto(this Database.Models.Feature feature) =>
		new()
		{
			FeatureId = feature.FeatureId,
			FeatureType = feature.FeatureTypeId,
			Name = feature.Name,
			ValueA = feature.ValueA,
			ValueB = feature.ValueB,
			CreatorUserId = feature.CreatorUserId,
			LastUpdatedTimestamp = feature.LastUpdatedTimestamp,
		};

	internal static Database.Models.Feature FromDto(this Feature f) =>
		new()
		{
			FeatureId = f.FeatureId,
			FeatureTypeId = f.FeatureType,
			Name = f.Name,
			ValueA = f.ValueA,
			ValueB = f.ValueB,
			CreatorUserId = f.CreatorUserId,
			LastUpdatedTimestamp = f.LastUpdatedTimestamp,
		};
}
