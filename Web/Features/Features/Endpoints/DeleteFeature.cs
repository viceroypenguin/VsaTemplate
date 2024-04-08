using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using SuperLinq;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Features.Endpoints;

[Handler]
[MapDelete("/api/features/{FeatureId}")]
public static partial class DeleteFeature
{
	public sealed record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Features;

		public required FeatureId FeatureId { get; set; }
	}

	private static async ValueTask<bool> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		var rowCount = await context.Features
			.Where(u => u.FeatureId == query.FeatureId)
			.DeleteAsync(token);
		return rowCount > 0;
	}
}
