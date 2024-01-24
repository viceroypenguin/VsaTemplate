using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Handlers;

[Handler]
[ApiController, Route("/api/users")]
public partial class GetActiveUsers(GetActiveUsers.Handler handler) : ControllerBase
{
	[HttpGet("active")]
	public ValueTask<IEnumerable<User>> Endpoint(CancellationToken token) =>
		handler.HandleAsync(new(), token);

	public record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Admin;
	}

	private static async ValueTask<IEnumerable<User>> HandleAsync(
			Query query,
			DbContext context,
			CancellationToken token) =>
		await context.Users
			.Where(u => u.IsActive)
			.SelectDto()
			.ToListAsync(token);
}
