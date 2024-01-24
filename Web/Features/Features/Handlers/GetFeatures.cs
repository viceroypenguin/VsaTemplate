using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Database;
using VsaTemplate.Web.Features.Features.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Features.Handlers;

[Handler]
[ApiController, Route("/api/features")]
public sealed partial class GetFeatures(GetFeatures.Handler handler)
{
	[HttpGet]
	public ValueTask<IEnumerable<Feature>> Endpoint(CancellationToken token) =>
		handler.HandleAsync(new(), token);

	public record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Features;
		public static IAuthorizationRequirement? Requirement => ResourceRequirement.Read;
	}

	private static async ValueTask<IEnumerable<Feature>> HandleAsync(
			Query _,
			DbContext context,
			CancellationToken token) =>
		await context.Features
			.SelectDto()
			.ToListAsync(token);
}
