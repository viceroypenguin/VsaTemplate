using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Features.Users.Queries;

namespace VsaTemplate.Web.Features.Users.Services;

[RegisterSingleton]
public sealed class UserRolesCache(
	IMemoryCache memoryCache,
	GetUserRoles.Handler getUserRoles
)
{
	private static string TransformKey(UserId userId) =>
		string.Create(CultureInfo.InvariantCulture, $"User-Roles-{userId}");

	private RolesValue GetRolesValue(UserId userId) =>
		memoryCache.GetOrCreate(
			TransformKey(userId),
			e => new RolesValue(userId, getUserRoles)
		)!;

	public ValueTask<IReadOnlyList<string>> GetUserRoles(UserId userId) =>
		GetRolesValue(userId).GetRoles();

	public void SetUserRoles(UserId userId, IReadOnlyList<string> roles) =>
		GetRolesValue(userId).SetRoles(roles);

	private sealed class RolesValue(
		UserId userId,
		GetUserRoles.Handler getUserRoles
	)
	{
		private IReadOnlyList<string>? _roles;
		private CancellationTokenSource? _tokenSource;
		private readonly object _lock = new();

		public async ValueTask<IReadOnlyList<string>> GetRoles()
		{
			if (_roles is not null)
				return _roles;

			var cts = new CancellationTokenSource();
			_tokenSource = cts;
			var token = cts.Token;

			try
			{
				var roles = await getUserRoles.HandleAsync(
					new() { UserId = userId },
					cancellationToken: token
				);

				lock (_lock)
					return _roles ??= roles;
			}
			catch (OperationCanceledException)
			{
				return _roles ?? [];
			}
		}

		public void SetRoles(IReadOnlyList<string> roles)
		{
			lock (_lock)
			{
				_roles = roles;
				_tokenSource?.Cancel();
			}
		}
	}
}
