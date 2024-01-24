using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperLinq;
using VsaTemplate.Database;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Features.Handlers;

[Handler]
[ApiController, Route("/api/features")]
public sealed partial class DeleteFeature(DeleteFeature.Handler handler) : ControllerBase
{
	[HttpDelete("{FeatureId}")]
	public async ValueTask Endpoint(Query query, CancellationToken token) =>
		await handler.HandleAsync(query, token);

	public record Query : IAuthorizedRequest
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
			.Where(u => u.FeatureId == query.FeatureId.Value)
			.DeleteAsync(token);
		return rowCount > 0;
	}
}
