using System.Data;
using Immediate.Handlers.Shared;
using LinqToDB;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Exceptions;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Features.AccessControl.Validators;

namespace VsaTemplate.Web.Features.AccessControl.Commands;

[Handler]
public sealed partial class EnsureEmailUser(DbContext context)
{
	public sealed record Command
	{
		public required string EmailAddress { get; init; }
	}

	private async ValueTask<UserId> HandleAsync(Command command, CancellationToken token)
	{
		var email = EmailLoginValidation.NormalizeEmail(command.EmailAddress);
		await using var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token);
		var user = await EnsureAsync(context, email, loginAt: null, token);

		if (user is null)
			throw new EmailLoginException();

		await transaction.CommitAsync(token);
		return user.UserId;
	}

	internal static async Task<EmailLoginUser?> EnsureAsync(
		DbContext context,
		string email,
		DateTimeOffset? loginAt,
		CancellationToken token
	)
	{

		var existing = await context.AccessControl.Users
			.TableHint("UPDLOCK")
			.Where(u => u.EmailAddress == email)
			.Select(u => new
			{
				u.UserId,
				u.Name,
				u.EmailAddress,
				u.IsActive,
				IsApiKey = u.ApiKey != null,
			})
			.FirstOrDefaultAsync(token);

		if (existing is { IsActive: false } or { IsApiKey: true })
			return null;

		if (existing is null)
		{

			var userId = UserId.From(
				await context.InsertWithInt32IdentityAsync(
					new Database.Models.AccessControl.User
					{
						EmailAddress = email,
						IsActive = true,
						LastLogin = loginAt,
					},
					token: token
				)
			);

			return new() { UserId = userId, Name = null, EmailAddress = email };
		}

		if (loginAt is { } time)
		{

			await context.AccessControl.Users
				.Where(u => u.UserId == existing.UserId)
				.Set(u => u.LastLogin, time)
				.UpdateAsync(token);

		}

		return new()
		{
			UserId = existing.UserId,
			Name = existing.Name,
			EmailAddress = existing.EmailAddress,
		};

	}
}
