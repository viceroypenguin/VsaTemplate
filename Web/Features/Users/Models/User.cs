using System.Linq.Expressions;
using System.Text.Json;

namespace VsaTemplate.Web.Features.Users.Models;

public sealed record User
{
	public UserId UserId { get; set; }
	public Auth0UserId? Auth0UserId { get; set; }
	public required string EmailAddress { get; set; }

	public string? Name { get; set; }
	public bool IsActive { get; set; }
	public DateTimeOffset? LastLogin { get; set; }

	public IReadOnlyList<string> Roles { get; set; } = [];

	public override int GetHashCode() =>
		UserId.GetHashCode();

	public bool Equals(User? other) =>
		other != null
		&& UserId.Equals(other.UserId);

	public static readonly Expression<Func<Database.Models.User, User>> FromDatabaseEntity =
		u => new()
		{
			UserId = u.UserId,
			Name = u.Name,
			Auth0UserId = u.Auth0UserId,
			EmailAddress = u.EmailAddress,
			IsActive = u.IsActive,
			LastLogin = u.LastLogin,
			Roles = ToRoles(u.Roles),
		};

	private static List<string> ToRoles(string roles) =>
		JsonSerializer.Deserialize<List<string>>(roles)!;
}
