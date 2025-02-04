using System.Text.Json;
using Immediate.Apis.Shared;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Shared.Exceptions;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.Authorization;

namespace VsaTemplate.Web.Features.Users.Endpoints;

[Handler]
[MapPut("/api/users")]
public static partial class UpdateUser
{
	[Validate]
	public sealed partial record Query : IAuthorizedRequest, IValidationTarget<Query>
	{
		public static string? Policy => Policies.Admin;

		public required UserId UserId { get; set; }

		[NotEmpty]
		public required string EmailAddress { get; init; }

		[NotEmpty]
		public required string Name { get; init; }

		public required bool IsActive { get; init; }
		public required IReadOnlyList<string> Roles { get; init; }
	}

	private static async ValueTask HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		var rows = await context.Users
			.Where(u => u.UserId == query.UserId)
			.UpdateAsync(
				u => new()
				{
					Name = query.Name,
					EmailAddress = query.EmailAddress,
					IsActive = query.IsActive,
					Roles = JsonSerializer.Serialize(query.Roles, JsonSerializerOptions.Default),
				},
				token: token
			);

		if (rows != 1)
			NotFoundException.ThrowNotFoundException("User");
	}
}
