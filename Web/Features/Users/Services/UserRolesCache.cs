using System.Globalization;
using System.Text.Json;
using Immediate.Cache;
using Immediate.Handlers.Shared;
using Immediate.Validations.Shared;
using LinqToDB;
using Microsoft.Extensions.Caching.Memory;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;

namespace VsaTemplate.Web.Features.Users.Services;

[RegisterSingleton]
public sealed class UserRolesCache(
	IMemoryCache memoryCache,
	Owned<IHandler<GetUserRoles.Query, IReadOnlyList<string>>> ownedGetUserRoles
) : ApplicationCacheBase<GetUserRoles.Query, IReadOnlyList<string>>(
	memoryCache,
	ownedGetUserRoles
)
{
	protected override string TransformKey(GetUserRoles.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"User-Roles-{request.UserId}");
}

[Handler]
public static partial class GetUserRoles
{
	[Validate]
	public sealed partial record Query : IValidationTarget<Query>
	{
		public required UserId UserId { get; set; }
	}

	private static async ValueTask<IReadOnlyList<string>> HandleAsync(
		Query query,
		DbContext context,
		CancellationToken token)
	{
		return JsonSerializer.Deserialize<IReadOnlyList<string>>(
			await context.Users
				.Where(u => u.UserId == query.UserId)
				.Select(u => u.Roles)
				.FirstAsync(token)
		) ?? [];
	}
}
