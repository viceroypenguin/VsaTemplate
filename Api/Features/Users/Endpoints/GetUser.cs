using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using Microsoft.AspNetCore.Authorization;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Shared.Extensions;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Infrastructure.Authorization;

namespace VsaTemplate.Api.Features.Users.Endpoints;

[Handler]
[MapGet("/api/users/{UserId}")]
[Authorize(Policy = Policies.Admin)]
public static partial class GetUser
{
	[Validate]
	public sealed partial record Query : IValidationTarget<Query>
	{
		public UserId UserId { get; set; }
	}

	private static async ValueTask<User> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token
	) =>
		await context.Users
			.Where(u => u.UserId == query.UserId)
			.SelectDto()
			.FirstNotFoundAsync("User", token);
}
