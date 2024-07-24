using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Endpoints;

[Handler]
[MapPost("/api/users/create")]
public static partial class CreateUser
{
	[Validate]
	public sealed partial record Command : IAuthorizedRequest, IValidationTarget<Command>
	{
		public static string? Policy => Policies.Admin;

		[NotEmpty]
		public required string EmailAddress { get; init; }

		[NotEmpty]
		public required string Name { get; init; }

		public required bool IsActive { get; init; }
		public required IReadOnlyList<string> Roles { get; init; }
	}

	private static async ValueTask<User> HandleAsync(
		Command query,
		DbContext context,
		CancellationToken token)
	{
		var user = await context.Users
			.InsertWithOutputAsync(
				new Database.Models.User
				{
					Name = query.Name,
					EmailAddress = query.EmailAddress,
					IsActive = query.IsActive,
					Roles = JsonSerializer.Serialize(query.Roles),
				},
				token: token);

		return user.ToDto();
	}
}
