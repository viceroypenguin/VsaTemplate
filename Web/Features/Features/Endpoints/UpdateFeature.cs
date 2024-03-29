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
[MapPut("/api/features")]
public static partial class UpdateFeature
{
	public sealed record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Features;

		public FeatureId FeatureId { get; set; }
		public required string Name { get; set; }
		public FeatureType FeatureType { get; set; }
		public UserId CreatorUserId { get; set; }
		public int ValueA { get; set; }
		public string? ValueB { get; set; }
	}

	private static async ValueTask<Feature> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		Guard.IsNotNull(query);
		Guard.IsGreaterThan(query.FeatureId.Value, 0);
		Guard.IsGreaterThan((int)query.FeatureType, 0);
		Guard.IsNotNullOrWhiteSpace(query.Name);
		Guard.IsGreaterThan(query.CreatorUserId.Value, 0);

		var output = await context.Features
			.Where(f => f.FeatureId == query.FeatureId.Value)
			.UpdateWithOutputAsync(
				u => new()
				{
					Name = query.Name,
					FeatureTypeId = (int)query.FeatureType,
					LastUpdatedTimestamp = DateTimeOffset.Now,
					ValueA = query.ValueA,
					ValueB = query.ValueB,
				},
				(d, i) => i,
				token);

		if (output.Length != 1)
			return ThrowHelper.ThrowInvalidOperationException<Feature>("Failed saving Feature");

		return output[0].ToDto();
	}
}
