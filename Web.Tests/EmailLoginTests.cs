using System.Net;
using System.Text.Json;
using System.Text;
using VsaTemplate.Web.Tests.Fixtures;

namespace VsaTemplate.Web.Tests;

public sealed class EmailLoginTests(ApplicationFactoryFixture fixture)
{
	private static CancellationToken Token => TestContext.Current.CancellationToken;

	[Fact]
	public async Task RequestSendsSixDigitCodeWithoutCreatingAUser()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var before = DateTimeOffset.UtcNow;

		using var response = await RequestCode(client, email, NewSession());
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);

		var body = await response.Content.ReadFromJsonAsync<JsonElement>(Token);
		var expiry = body.GetProperty("expiresAt").GetDateTimeOffset();

		Assert.InRange(expiry, before.AddMinutes(15), DateTimeOffset.UtcNow.AddMinutes(15));
		Assert.Matches("^[0-9]{6}$", fixture.Emails.ReadCode(email));
		Assert.False(await fixture.UserExistsAsync(email));
	}

	[Fact]
	public async Task CodeFromTheSameSessionSignsInAndCanOnlyBeUsedOnce()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var session = NewSession();
		using var issued = await RequestCode(client, email, session);
		issued.EnsureSuccessStatusCode();
		var code = fixture.Emails.ReadCode(email);
		Assert.StartsWith("0", code, StringComparison.Ordinal);

		using var verified = await VerifyCode(client, email, session, code);
		Assert.Equal(HttpStatusCode.NoContent, verified.StatusCode);
		Assert.True(await fixture.UserExistsAsync(email));

		using var status = await client.GetAsync(new Uri("/api/session", UriKind.Relative), Token);
		var body = await status.Content.ReadFromJsonAsync<JsonElement>(Token);
		Assert.True(body.GetProperty("isAuthenticated").GetBoolean());
		Assert.Equal(email, body.GetProperty("user").GetProperty("emailAddress").GetString());

		using var replayClient = await fixture.CreateEmailLoginClientAsync();
		using var replay = await VerifyCode(replayClient, email, session, code);
		Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
	}

	[Fact]
	public async Task CodeCannotBeRedeemedFromAnotherSessionOrForAnotherEmail()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var session = NewSession();
		using var issued = await RequestCode(client, email, session);
		issued.EnsureSuccessStatusCode();
		var code = fixture.Emails.ReadCode(email);

		using var wrongSession = await VerifyCode(client, email, NewSession(), code);
		Assert.Equal(HttpStatusCode.BadRequest, wrongSession.StatusCode);

		using var wrongEmail = await VerifyCode(client, NewEmail(), session, code);
		Assert.Equal(HttpStatusCode.BadRequest, wrongEmail.StatusCode);

		using var correct = await VerifyCode(client, email, session, code);
		Assert.Equal(HttpStatusCode.NoContent, correct.StatusCode);
	}

	[Fact]
	public async Task FiveIncorrectCodesInvalidateTheChallenge()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var session = NewSession();
		using var issued = await RequestCode(client, email, session);
		issued.EnsureSuccessStatusCode();
		var code = fixture.Emails.ReadCode(email);
		var wrongCode = code == "999999" ? "888888" : "999999";

		for (var attempt = 0; attempt < 5; attempt++)
		{
			using var rejected = await VerifyCode(client, email, session, wrongCode);
			Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
		}

		using var exhausted = await VerifyCode(client, email, session, code);
		Assert.Equal(HttpStatusCode.BadRequest, exhausted.StatusCode);
		Assert.False(await fixture.UserExistsAsync(email));
	}

	[Fact]
	public async Task ExpiredCodeCannotSignIn()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var session = NewSession();
		using var issued = await RequestCode(client, email, session);
		issued.EnsureSuccessStatusCode();
		await fixture.AgeLoginChallengeAsync(session, TimeSpan.FromMinutes(16));

		using var expired = await VerifyCode(client, email, session, fixture.Emails.ReadCode(email));
		Assert.Equal(HttpStatusCode.BadRequest, expired.StatusCode);
		Assert.False(await fixture.UserExistsAsync(email));
	}

	[Fact]
	public async Task ResendCooldownAppliesToBothTheSessionAndTheEmailAddress()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var session = NewSession();
		using var issued = await RequestCode(client, email, session);
		issued.EnsureSuccessStatusCode();

		using var sameSession = await RequestCode(client, NewEmail(), session);
		Assert.Equal(HttpStatusCode.TooManyRequests, sameSession.StatusCode);
		Assert.NotNull(sameSession.Headers.RetryAfter);

		using var newSession = await RequestCode(client, email, NewSession());
		Assert.Equal(HttpStatusCode.TooManyRequests, newSession.StatusCode);
	}

	[Fact]
	public async Task ResendingReplacesTheOldCode()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var session = NewSession();
		using var issued = await RequestCode(client, email, session);
		issued.EnsureSuccessStatusCode();
		var oldCode = fixture.Emails.ReadCode(email);
		await fixture.AgeLoginChallengeAsync(session, TimeSpan.FromSeconds(61));

		using var resend = await RequestCode(client, email, session);
		resend.EnsureSuccessStatusCode();
		var newCode = fixture.Emails.ReadCode(email);
		Assert.NotEqual(oldCode, newCode, StringComparer.Ordinal);

		using var oldAttempt = await VerifyCode(client, email, session, oldCode);
		Assert.Equal(HttpStatusCode.BadRequest, oldAttempt.StatusCode);

		using var freshAttempt = await VerifyCode(client, email, session, newCode);
		Assert.Equal(HttpStatusCode.NoContent, freshAttempt.StatusCode);
	}

	[Fact]
	public async Task ConcurrentRedemptionOnlySignsInOneClient()
	{
		using var first = await fixture.CreateEmailLoginClientAsync();
		using var second = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var session = NewSession();
		using var issued = await RequestCode(first, email, session);
		issued.EnsureSuccessStatusCode();
		var code = fixture.Emails.ReadCode(email);

		var responses = await Task.WhenAll(
			VerifyCode(first, email, session, code),
			VerifyCode(second, email, session, code)
		);

		try
		{

			Assert
				.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);

			Assert
				.Single(responses, response => response.StatusCode == HttpStatusCode.BadRequest);

		}
		finally
		{

			foreach (var response in responses)
				response.Dispose();

		}

	}

	[Fact]
	public async Task AnInactiveAccountCannotSignInWithAValidCode()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var userId = await fixture.CreateUserAsync(emailAddress: email);
		await fixture.DeactivateUserAsync(userId);
		var session = NewSession();
		using var issued = await RequestCode(client, email, session);
		issued.EnsureSuccessStatusCode();

		using var verified = await VerifyCode(client, email, session, fixture.Emails.ReadCode(email));
		Assert.Equal(HttpStatusCode.BadRequest, verified.StatusCode);

		using var status = await client.GetAsync(new Uri("/api/session", UriKind.Relative), Token);
		var body = await status.Content.ReadFromJsonAsync<JsonElement>(Token);
		Assert.False(body.GetProperty("isAuthenticated").GetBoolean());
	}

	[Fact]
	public async Task AnonymousLoginRequestsRequireAntiforgeryValidation()
	{
		using var client = fixture.CreateAnonymousClient();
		using var request = await RequestCode(client, NewEmail(), NewSession());
		Assert.Equal(HttpStatusCode.BadRequest, request.StatusCode);

		using var verify = await VerifyCode(client, NewEmail(), NewSession(), "123456");
		Assert.Equal(HttpStatusCode.BadRequest, verify.StatusCode);
	}

	[Fact]
	public async Task MalformedOrIncompleteJsonIsAClientError()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		using var invalidJson = new StringContent("{", Encoding.UTF8, "application/json");

		using var malformed = await client.PostAsync(new Uri("/api/auth/email/request", UriKind.Relative), invalidJson, Token);
		Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);

		using var missingFields = await client.PostAsJsonAsync(new Uri("/api/auth/email/verify", UriKind.Relative), new { }, Token);
		Assert.Equal(HttpStatusCode.BadRequest, missingFields.StatusCode);
	}

	[Fact]
	public async Task DeliveryFailureDoesNotCreateAnAccountAndCanBeRetried()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var session = NewSession();
		fixture.Emails.FailFor(email);

		using var failed = await RequestCode(client, email, session);
		Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
		Assert.False(await fixture.UserExistsAsync(email));
		fixture.Emails.ResumeFor(email);

		using var retried = await RequestCode(client, email, session);
		Assert.Equal(HttpStatusCode.OK, retried.StatusCode);
	}

	[Fact]
	public async Task LogoutClearsTheEmailLoginCookie()
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		var email = NewEmail();
		var session = NewSession();
		using var issued = await RequestCode(client, email, session);
		issued.EnsureSuccessStatusCode();
		using var verified = await VerifyCode(client, email, session, fixture.Emails.ReadCode(email));
		verified.EnsureSuccessStatusCode();
		await ApplicationFactoryFixture.RefreshAntiforgeryAsync(client);

		using var logout = await client.PostAsync(new Uri("/Logout", UriKind.Relative), content: null, Token);
		Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);

		using var status = await client.GetAsync(new Uri("/api/session", UriKind.Relative), Token);
		var body = await status.Content.ReadFromJsonAsync<JsonElement>(Token);
		Assert.False(body.GetProperty("isAuthenticated").GetBoolean());
	}

	[Theory]
	[InlineData("not-an-email", "bed177d7-7d54-4cdc-b872-dc42407b05db")]
	[InlineData("user@example.test", "not-a-session")]
	public async Task MalformedEmailOrSessionIsRejected(string email, string session)
	{
		using var client = await fixture.CreateEmailLoginClientAsync();
		using var response = await RequestCode(client, email, session);
		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	private static string NewEmail() => $"login-{Guid.NewGuid():N}@example.test";
	private static string NewSession() => Guid.NewGuid().ToString("D");

	private static Task<HttpResponseMessage> RequestCode(HttpClient client, string email, string session) =>
		client.PostAsJsonAsync(new Uri("/api/auth/email/request", UriKind.Relative), new { emailAddress = email, sessionId = session }, Token);

	private static Task<HttpResponseMessage> VerifyCode(HttpClient client, string email, string session, string code) =>
		client.PostAsJsonAsync(new Uri("/api/auth/email/verify", UriKind.Relative), new { emailAddress = email, sessionId = session, token = code }, Token);
}
