using CommunityToolkit.Diagnostics;
using Immediate.Handlers.Shared;
using LinqToDB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VsaTemplate.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Handlers;

[Handler]
[ApiController, Route("/api/users")]
public sealed partial class CreateUser(CreateUser.Handler handler) : ControllerBase
{
	[HttpPost("create")]
	public async ValueTask<User> Endpoint([FromBody] Query query, CancellationToken token) =>
		await handler.HandleAsync(query, token);

	public record Query : IAuthorizedRequest
	{
		public static string? Policy => Policies.Admin;
		public static IAuthorizationRequirement? Requirement => ResourceRequirement.Update;

		public required string EmailAddress { get; init; }
		public required string Name { get; init; }
		public bool IsActive { get; init; }
	}

	private static async ValueTask<User> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		Guard.IsNotNullOrWhiteSpace(query.Name);
		Guard.IsNotNullOrWhiteSpace(query.EmailAddress);

		var user = await context.Users
			.InsertWithOutputAsync(
				new Database.Models.User
				{
					Name = query.Name,
					EmailAddress = query.EmailAddress,
					IsActive = query.IsActive,
				},
				token: token);

		return user.ToDto();
	}
}
