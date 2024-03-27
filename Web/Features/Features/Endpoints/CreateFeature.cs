using CommunityToolkit.Diagnostics;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using SuperLinq;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Features.Models;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Features.Endpoints;

[Handler]
[MapPost("/api/features/create")]
public static partial class CreateFeature
{
	public sealed record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Features;

		public required string Name { get; init; }
		public required FeatureType FeatureType { get; init; }
		public required UserId CreatorUserId { get; init; }
		public required int ValueA { get; init; }
		public required string? ValueB { get; init; }
	}

	private static async ValueTask<Feature> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		Guard.IsNotNull(query);
		Guard.IsGreaterThan((int)query.FeatureType, 0);
		Guard.IsNotNullOrWhiteSpace(query.Name);
		Guard.IsGreaterThan(query.CreatorUserId.Value, 0);

		var output = await SuperEnumerable.Return(
				new
				{
					query.Name,
					query.FeatureType,
					CreatorUserId = query.CreatorUserId.Value,
					query.ValueA,
					query.ValueB,
				})
			.AsQueryable(context)
			.InsertWithOutputAsync(
				context.Features,
				src => new()
				{
					Name = src.Name,
					FeatureTypeId = (int)src.FeatureType,
					CreatorUserId = src.CreatorUserId,
					LastUpdatedTimestamp = DateTimeOffset.Now,
					ValueA = src.ValueA,
					ValueB = src.ValueB,
				},
				token);

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
			ValueA = query.ValueA,
			ValueB = query.ValueB,
		};
	}
}
