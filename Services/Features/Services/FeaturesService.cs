using CommunityToolkit.Diagnostics;
using LinqToDB;
using SuperLinq;
using VsaTemplate.Database;
using VsaTemplate.Features.Models;
using VsaTemplate.Users.Models;

namespace VsaTemplate.Features.Services;

[RegisterScoped]
public class FeaturesService
{
	private readonly DbContext _context;

	public FeaturesService(DbContext context)
	{
		_context = context;
	}

	public async Task<IReadOnlyList<Feature>> GetFeatures() =>
		await _context.Features
			.Select(f => new Feature()
			{
				FeatureId = FeatureId.From(f.FeatureId),
				Name = f.Name,
				CreatorUserId = UserId.From(f.CreatorUserId),
				LastUpdatedTimestamp = f.LastUpdatedTimestamp,
				FeatureType = (FeatureType)f.FeatureTypeId,
				ValueA = f.ValueA,
				ValueB = f.ValueB,

				CreatorName = f.CreatorUser.Name,
			})
			.ToListAsync();

	public async Task<Feature> CreateFeature(CreateFeatureDto feature)
	{
		Guard.IsNotNull(feature);
		Guard.IsGreaterThan((int)feature.FeatureType, 0);
		Guard.IsNotNullOrWhiteSpace(feature.Name);
		Guard.IsGreaterThan(feature.CreatorUserId.Value, 0);

		var output = await SuperEnumerable.Return(
				new
				{
					feature.Name,
					feature.FeatureType,
					CreatorUserId = feature.CreatorUserId.Value,
					feature.ValueA,
					feature.ValueB,
				})
			.AsQueryable(_context)
			.InsertWithOutputAsync(
				_context.Features,
				src => new()
				{
					Name = src.Name,
					FeatureTypeId = (int)src.FeatureType,
					CreatorUserId = src.CreatorUserId,
					LastUpdatedTimestamp = DateTimeOffset.Now,
					ValueA = src.ValueA,
					ValueB = src.ValueB,
				});

		if (output.Length != 1)
			return ThrowHelper.ThrowInvalidOperationException<Feature>("Failed saving Feature");

		var f = output[0];
		return new()
		{
			FeatureId = FeatureId.From(f.FeatureId),
			Name = f.Name,
			FeatureType = (FeatureType)f.FeatureTypeId,
			CreatorUserId = UserId.From(f.CreatorUserId),
			LastUpdatedTimestamp = f.LastUpdatedTimestamp,
			ValueA = feature.ValueA,
			ValueB = feature.ValueB,
		};
	}

	public async Task<Feature> UpdateFeature(Feature feature)
	{
		Guard.IsNotNull(feature);
		Guard.IsGreaterThan(feature.FeatureId.Value, 0);
		Guard.IsGreaterThan((int)feature.FeatureType, 0);
		Guard.IsNotNullOrWhiteSpace(feature.Name);
		Guard.IsGreaterThan(feature.CreatorUserId.Value, 0);

		var output = await _context.Features
			.Where(f => f.FeatureId == feature.FeatureId.Value)
			.UpdateWithOutputAsync(
				u => new()
				{
					Name = feature.Name,
					FeatureTypeId = (int)feature.FeatureType,
					LastUpdatedTimestamp = DateTimeOffset.Now,
					ValueA = feature.ValueA,
					ValueB = feature.ValueB,
				},
				(d, i) => i);

		if (output.Length != 1)
			return ThrowHelper.ThrowInvalidOperationException<Feature>("Failed saving Feature");

		return feature with
		{
			LastUpdatedTimestamp = output[0].LastUpdatedTimestamp,
		};
	}

	public async Task<bool> DeleteFeature(FeatureId featureId)
	{
		var rowCount = await _context.Features
			.Where(u => u.FeatureId == featureId.Value)
			.DeleteAsync();
		return rowCount > 0;
	}
}
