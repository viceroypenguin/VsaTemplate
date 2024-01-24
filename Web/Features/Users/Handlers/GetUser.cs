using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Handlers;

[Handler]
[ApiController, Route("/api/users")]
public partial class GetUser(GetUser.Handler handler) : ControllerBase
{
	[HttpGet("{UserId}")]
	public ValueTask<User> Endpoint(Query query, CancellationToken token) =>
		handler.HandleAsync(query, token);

	public record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Admin;

		public int UserId { get; set; }
	}

	private static async ValueTask<User> HandleAsync(
			Query query,
			DbContext context,
			CancellationToken token) =>
		await context.Users
			.Where(u => u.UserId == query.UserId)
			.SelectDto()
			.FirstAsync(token);
}
