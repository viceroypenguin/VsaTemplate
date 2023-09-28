using CommunityToolkit.Diagnostics;
using LinqToDB;
using SuperLinq;
using VsaTemplate.Database;
using VsaTemplate.Users.Models;

namespace VsaTemplate.Users.Services;

[RegisterScoped]
public class UsersService
{
	private readonly DbContext _context;

	public UsersService(DbContext context)
	{
		_context = context;
	}

	public async Task<IReadOnlyList<User>> GetUsers() =>
		await _context.Users
			.Select(u => new User()
			{
				UserId = (UserId)u.UserId,
				Auth0UserId = u.Auth0UserId != null ? (Auth0UserId?)u.Auth0UserId : default,
				Name = u.Name ?? string.Empty,
				EmailAddress = u.EmailAddress,
				IsActive = u.IsActive,
			})
			.ToListAsync();

	public async Task<IReadOnlyList<User>> GetActiveUsers() =>
		await _context.Users
			.Where(u => u.IsActive)
			.Select(u => new User()
			{
				UserId = (UserId)u.UserId,
				Auth0UserId = u.Auth0UserId != null ? (Auth0UserId?)u.Auth0UserId : default,
				Name = u.Name ?? string.Empty,
				EmailAddress = u.EmailAddress,
				IsActive = u.IsActive,
			})
			.ToListAsync();

	public async Task<User> GetUser(UserId userId) =>
		await _context.Users
			.Where(u => u.UserId == userId.Value)
			.Select(u => new User()
			{
				UserId = (UserId)u.UserId,
				Auth0UserId = u.Auth0UserId != null ? (Auth0UserId?)u.Auth0UserId : default,
				Name = u.Name ?? string.Empty,
				EmailAddress = u.EmailAddress,
				IsActive = u.IsActive,
			})
			.FirstAsync();

	public async Task<UserId> GetOrCreateUserId(Auth0UserId userId, string emailAddress)
	{
		Guard.IsNotNull(userId.Value);
		Guard.IsNotNull(emailAddress);

		var merges = await _context.Users
			.Merge().Using(SuperEnumerable.Return(new { UserId = userId.Value, EmailAddress = emailAddress }))
			.On((dst, src) => dst.EmailAddress == src.EmailAddress)
			.InsertWhenNotMatched(src =>
				new Database.Models.User
				{
					EmailAddress = src.EmailAddress,
					Auth0UserId = src.UserId,
					IsActive = true,
				})
			.UpdateWhenMatched((dst, src) =>
				new Database.Models.User
				{
					Auth0UserId = src.UserId,
				})
			.MergeWithOutputAsync((a, d, i) => new { i.UserId, i.IsActive, })
			.ToListAsync();

		if (merges.Count != 1)
			return ThrowHelper.ThrowInvalidOperationException<UserId>("Failed saving user");

		if (!merges[0].IsActive)
			return ThrowHelper.ThrowInvalidOperationException<UserId>("User is not active.");

		return UserId.From(merges[0].UserId);
	}

	public async Task<User> CreateUser(CreateUserDto user)
	{
		Guard.IsNotNull(user);
		Guard.IsNotNullOrWhiteSpace(user.Name);
		Guard.IsNotNullOrWhiteSpace(user.EmailAddress);

		var userId = await _context.InsertWithInt32IdentityAsync(
			new Database.Models.User
			{
				Name = user.Name,
				EmailAddress = user.EmailAddress,
				IsActive = user.IsActive,
			});

		return new()
		{
			UserId = UserId.From(userId),
			Name = user.Name,
			EmailAddress = user.EmailAddress,
			IsActive = user.IsActive,
		};
	}

	public async Task<User> UpdateUser(User user)
	{
		Guard.IsNotNull(user);
		Guard.IsGreaterThan(user.UserId.Value, 0);
		Guard.IsNotNullOrWhiteSpace(user.Name);
		Guard.IsNotNullOrWhiteSpace(user.EmailAddress);

		var cnt = await _context.Users
			.Where(u => u.UserId == user.UserId.Value)
			.UpdateAsync(u => new()
			{
				Name = user.Name,
				EmailAddress = user.EmailAddress,
				IsActive = user.IsActive,
			});

		if (cnt != 1)
			return ThrowHelper.ThrowInvalidOperationException<User>("Failed saving user");

		return user;
	}
}
