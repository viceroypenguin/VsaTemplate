using System.Data;
using System.Security.Cryptography;
using System.Text;
using Immediate.Injections.Shared;
using LinqToDB;
using LinqToDB.Async;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Exceptions;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Infrastructure.Emails;

namespace VsaTemplate.Web.Features.AccessControl.Services;

[RegisterScoped]
public sealed class EmailLoginService(
	DbContext context,
	TimeProvider timeProvider,
	IEmailSender emailSender,
	ILoginTokenGenerator tokenGenerator
)
{
	private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
	private static readonly TimeSpan RequestCooldown = TimeSpan.FromMinutes(1);
	private const int MaximumAttempts = 5;

	public async Task<DateTimeOffset> RequestAsync(string emailAddress, string sessionId, CancellationToken token)
	{
		var email = EmailLoginNormalization.NormalizeEmail(emailAddress);
		var session = EmailLoginNormalization.NormalizeSession(sessionId);

		var sessionHash = HashSession(session);

		var now = timeProvider.GetUtcNow();
		var expiresAt = now.Add(Lifetime);

		var code = tokenGenerator.GenerateToken();
		var codeHash = HashCode(session, email, code);

		await using (var transaction = await context.BeginTransactionAsync(IsolationLevel.Serializable, token))
		{
			await context.AccessControl.EmailLoginChallenges
				.Where(c => c.ExpiresAt <= now)
				.DeleteAsync(token);

			var recent = await context.AccessControl.EmailLoginChallenges
				.TableHint("UPDLOCK")
				.Where(c => c.CreatedAt > now.Subtract(RequestCooldown))
				.Where(c => c.SessionIdHash == sessionHash || c.EmailAddress == email)
				.OrderByDescending(c => c.CreatedAt)
				.Select(c => (DateTimeOffset?)c.CreatedAt)
				.FirstOrDefaultAsync(token);

			if (recent is { } issuedAt)
			{
				throw new EmailLoginRateLimitException(
					Math.Max(1, (int)Math.Ceiling(issuedAt.Add(RequestCooldown).Subtract(now).TotalSeconds))
				);
			}

			var previousHash = await context.AccessControl.EmailLoginChallenges
				.TableHint("UPDLOCK")
				.Where(c => c.SessionIdHash == sessionHash)
				.Select(c => c.TokenHash)
				.FirstOrDefaultAsync(token);

			for (var attempt = 0; previousHash is not null && CryptographicOperations.FixedTimeEquals(previousHash, codeHash); attempt++)
			{
				if (attempt >= 10)
					throw new InvalidOperationException("The sign-in code generator repeatedly returned the previous code.");

				code = tokenGenerator.GenerateToken();
				codeHash = HashCode(session, email, code);
			}

			await context.AccessControl.EmailLoginChallenges
				.Where(c => c.SessionIdHash == sessionHash)
				.DeleteAsync(token);

			await context.InsertAsync(
				new Database.Models.AccessControl.EmailLoginChallenge
				{
					SessionIdHash = sessionHash,
					EmailAddress = email,
					TokenHash = codeHash,
					CreatedAt = now,
					ExpiresAt = expiresAt,
					FailedAttempts = 0,
					IsConsumed = false,
				},
				token: token
			);

			await transaction.CommitAsync(token);
		}

		try
		{
			await emailSender.SendLoginEmail(
				new()
				{
					To = [email],
					Subject = "Your sign-in code",
					Body = $"Your sign-in code is {code}.\n\nEnter it in the same window where you requested it. This code expires in 15 minutes and can be used once.\n\nIf you did not request this email, you can ignore it.",
				},
				token
			);
		}
		catch (Exception)
		{
			// Do not leave a usable challenge after a failed delivery or disconnect.

			await context.AccessControl.EmailLoginChallenges
				.Where(c => c.SessionIdHash == sessionHash && c.TokenHash == codeHash)
				.DeleteAsync(CancellationToken.None);

			throw new EmailLoginDeliveryException();
		}

		return expiresAt;
	}

	private sealed record EmailUser
	{
		public string EmailAddress { get; init; } = null!;

		public bool IsActive { get; init; }

		public DateTimeOffset? LastLogin { get; init; }

		public string? Name { get; init; }

		public UserId UserId { get; init; }

		public bool IsApiKey { get; init; }
	}

	public async Task<EmailLoginUser?> EnsureAsync(
		string email,
		DateTimeOffset? loginAt,
		CancellationToken token
	)
	{
		var output = await context.AccessControl.Users
			.Where(u => u.EmailAddress == email)
			.Select(u => new EmailUser
			{
				UserId = u.UserId,
				EmailAddress = u.EmailAddress,
				Name = u.Name,
				LastLogin = u.LastLogin,
				IsActive = u.IsActive,
				IsApiKey = context.AccessControl.ApiKeys.Any(ak => ak.ApiKeyId == u.UserId),
			})
			.AsCte()
			.Merge().Using([new { EmailAddress = email, LoginTime = loginAt }])
			.On((dst, src) => dst.EmailAddress == src.EmailAddress)
			.InsertWhenNotMatched(src => new()
			{
				EmailAddress = src.EmailAddress,
				IsActive = true,
				LastLogin = src.LoginTime,
			})
			.UpdateWhenMatched(
				(dst, src) => new()
				{
					LastLogin = src.LoginTime ?? dst.LastLogin,
				}
			)
			.MergeWithOutputAsync(
				(a, del, ins, src) => new
				{
					Action = a,
					ins.UserId,
					ins.EmailAddress,
					ins.Name,
					ins.IsActive,
					ins.IsApiKey,
				}
			)
			.ToListAsync(token);

		if (output is not [{ } existing])
			throw new InvalidOperationException("Exactly one row should be returned by the merge.");

		if (existing is { IsActive: false } or { IsApiKey: true })
			return null;

		return new()
		{
			UserId = existing.UserId,
			Name = existing.Name,
			EmailAddress = existing.EmailAddress,
		};
	}

	public async Task<EmailLoginUser> VerifyAsync(
		string emailAddress,
		string sessionId,
		string code,
		CancellationToken token
	)
	{
		var email = EmailLoginNormalization.NormalizeEmail(emailAddress);
		var session = EmailLoginNormalization.NormalizeSession(sessionId);

		var sessionHash = HashSession(session);
		var codeHash = HashCode(session, email, code);

		await using var transaction = await context.BeginTransactionAsync(token);

		var challenge = await context.AccessControl.EmailLoginChallenges
			.TableHint("UPDLOCK")
			.FirstOrDefaultAsync(c => c.SessionIdHash == sessionHash, token);

		var now = timeProvider.GetUtcNow();

		if (challenge is null
			|| challenge.EmailAddress != email
			|| challenge.ExpiresAt <= now
			|| challenge.IsConsumed
			|| challenge.FailedAttempts >= MaximumAttempts)
		{
			throw new EmailLoginException();
		}

		if (!CryptographicOperations.FixedTimeEquals(challenge.TokenHash, codeHash))
		{
			var attempts = challenge.FailedAttempts + 1;

			await context.AccessControl.EmailLoginChallenges
				.Where(c => c.SessionIdHash == sessionHash)
				.Set(c => c.FailedAttempts, attempts)
				.Set(c => c.IsConsumed, attempts >= MaximumAttempts)
				.UpdateAsync(CancellationToken.None);

			// Failed attempts must survive the error response and a client disconnect.
			await transaction.CommitAsync(CancellationToken.None);

			throw new EmailLoginException();
		}

		await context.AccessControl.EmailLoginChallenges
			.Where(c => c.SessionIdHash == sessionHash)
			.Set(c => c.IsConsumed, value: true)
			.UpdateAsync(CancellationToken.None);

		var user = await EnsureAsync(email, now, CancellationToken.None);

		await transaction.CommitAsync(CancellationToken.None);

		return user ?? throw new EmailLoginException();
	}

	private static byte[] HashSession(string sessionId) =>
		SHA256.HashData(Encoding.UTF8.GetBytes(sessionId));

	private static byte[] HashCode(string sessionId, string emailAddress, string code) =>
		SHA256.HashData(Encoding.UTF8.GetBytes($"{sessionId}\n{emailAddress}\n{code}"));
}
