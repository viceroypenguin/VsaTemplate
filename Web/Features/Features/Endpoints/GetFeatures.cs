using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Features.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Features.Endpoints;

[Handler]
[MapGet("/api/features")]
public static partial class GetFeatures
{
	public sealed record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Features;
	}

	private static async ValueTask<IEnumerable<Feature>> HandleAsync(
			Query _,
			DbContext context,
			CancellationToken token) =>
		await context.Features
			.SelectDto()
			.ToListAsync(token);
}
