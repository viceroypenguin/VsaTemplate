using System.Globalization;
using System.Text.Json;
using Immediate.Cache.Shared;
using Immediate.Handlers.Shared;
using LinqToDB.Async;
using SuperLinq;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.Organizations.Models;

namespace VsaTemplate.Web.Features.Organizations.Services;

[CacheFor<OrganizationGetUserPermissions>]
public sealed partial class OrganizationUserPermissionCache
{
	protected override string TransformKey(OrganizationGetUserPermissions.Query request) =>
		string.Create(CultureInfo.InvariantCulture, $"OrganizationUserPermissionCache-{request.UserId}-{request.OrganizationId}");
}

[Handler]
public sealed partial class OrganizationGetUserPermissions(
	DbContext context,
	OrganizationUserPermissionCache organizationUserPermissionCache
)
{
	public sealed record Query
	{
		public required UserId UserId { get; init; }
		public required OrganizationId OrganizationId { get; init; }
	}

	public sealed record Response
	{
		public required IReadOnlyList<OrganizationPermission> Permissions { get; init; }
	}

	private async ValueTask<Response> HandleAsync(
		Query query,
		CancellationToken token
	)
	{
		var apiKey = await context.AccessControl.ApiKeys
			.FirstOrDefaultAsync(u => u.ApiKeyId == query.UserId, token);

		if (apiKey is null)
		{
			return new()
			{
				Permissions = (
					await context.OrganizationRoleUsers
						.Where(u => u.UserId == query.UserId)
						.Select(u => u.OrganizationRole)
						.Where(r => r.OrganizationId == query.OrganizationId)
						.Select(r => JsonSerializer.Deserialize<List<OrganizationPermission>>(r.PermissionsJson, default(JsonSerializerOptions))!)
						.ToListAsync(token)
				)
					.SelectMany(SuperEnumerable.Identity)
					.DefaultIfEmpty(OrganizationPermission.None)
					.Distinct()
					.ToList(),
			};
		}

		var parentPermissions = await organizationUserPermissionCache.GetValue(
			new() { UserId = apiKey.OwnerUserId, OrganizationId = query.OrganizationId },
			token
		);

		return new()
		{
			Permissions = JsonSerializer.Deserialize<List<OrganizationPermission>>(apiKey.PermissionsJson, default(JsonSerializerOptions))!
				.Intersect(parentPermissions.Permissions)
				.ToList(),
		};
	}
}
