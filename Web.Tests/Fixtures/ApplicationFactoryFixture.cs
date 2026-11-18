using System.Globalization;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Commands;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Infrastructure.Authentication;
using VsaTemplate.Web.Infrastructure.Emails;
using VsaTemplate.Web.Features.AccessControl.Services;
using VsaTemplate.Web.Tests.Fixtures;

[assembly: AssemblyFixture(typeof(ApplicationFactoryFixture))]

namespace VsaTemplate.Web.Tests.Fixtures;

public sealed class ApplicationFactoryFixture : IAsyncLifetime
{
	private readonly MsSqlContainer _container =
		new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")
			.WithEnvironment("MSSQL_MEMORY_LIMIT_MB", "2048")
			.Build();

	// Any public type in the application assembly can identify its entry point.
	private WebApplicationFactory<DbContext>? _factory;
	private string? _externalServer;
	private string? _externalDatabase;

	public CapturingEmailSender Emails { get; } = new();

	public async ValueTask InitializeAsync()
	{
		var connectionString = await GetConnectionString();
		_factory = new TestWebApplicationFactory(connectionString, Emails);
		_factory.StartServer();
	}

	private async Task<string> GetConnectionString()
	{
		var connectionFile = Environment.GetEnvironmentVariable("VSA_TEST_SQL_CONNECTION_FILE");

		if (string.IsNullOrWhiteSpace(connectionFile))
		{
			await _container.StartAsync(TestContext.Current.CancellationToken);
			return _container.GetConnectionString();
		}
		else
		{
			_externalServer = await File.ReadAllTextAsync(connectionFile, TestContext.Current.CancellationToken);
			_externalDatabase = $"VsaTemplate_Tests_{Guid.NewGuid():N}";

			await ExecuteDatabaseCommandAsync(_externalServer, _externalDatabase, create: true);

			var builder = new SqlConnectionStringBuilder(_externalServer)
			{
				InitialCatalog = _externalDatabase,
			};

			return builder.ConnectionString;
		}
	}

	public async Task<UserId> CreateUserAsync(string name = "Test User", bool isAdministrator = false, string? emailAddress = null)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<DbContext>();

		var userId = UserId.From(
			await context.InsertWithInt32IdentityAsync(
				new Database.Models.AccessControl.User
				{
					Name = name,
					EmailAddress = emailAddress ?? $"{Guid.NewGuid():N}@example.test",
					IsActive = true,
				},
				token: TestContext.Current.CancellationToken
			)
		);

		if (isAdministrator)
		{
			await context.InsertAsync(
				new Database.Models.AccessControl.RoleUser
				{
					UserId = userId,
					RoleId = RoleId.From(-1),
					EditedUserId = userId,
				},
				token: TestContext.Current.CancellationToken
			);
		}

		return userId;
	}

	public async Task<UserId> EnsureEmailUserAsync(string emailAddress)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var handler = scope.ServiceProvider.GetRequiredService<EnsureEmailUser.Handler>();

		return await handler.HandleAsync(new() { EmailAddress = emailAddress }, TestContext.Current.CancellationToken);
	}

	public async Task<string> CreateApiKeyAsync(UserId ownerId, DateTimeOffset expiresAt)
	{
		var key = $"test-key-{Guid.NewGuid():N}";
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<DbContext>();

		var keyId = UserId.From(
			await context.InsertWithInt32IdentityAsync(new Database.Models.AccessControl.User
			{
				Name = "Test API key",
				EmailAddress = key,
				IsActive = true,
			},
			token: TestContext.Current.CancellationToken)
		);

		await context.InsertAsync(
			new Database.Models.AccessControl.ApiKey
			{
				ApiKeyId = keyId,
				OwnerUserId = ownerId,
				CreatedDateTime = expiresAt.AddHours(-1),
				ExpirationDateTime = expiresAt,
				PermissionsJson = "[0]",
			},
			token: TestContext.Current.CancellationToken
		);

		return key;
	}

	public async Task<IReadOnlyList<Permission>> ReadApiKeyPermissionsAsync(string key)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<DbContext>();

		var json = await context.AccessControl.ApiKeys
			.Where(k => k.User.EmailAddress == key)
			.Select(k => k.PermissionsJson)
			.SingleAsync(TestContext.Current.CancellationToken);

		return JsonSerializer.Deserialize<Permission[]>(json)!;
	}

	public async Task AddApiKeyScopePermissionsAsync(UserId keyId)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<DbContext>();
		var token = TestContext.Current.CancellationToken;

		await context.InsertAsync(new Database.Models.TenantApiKey
		{
			TenantApiKeyId = keyId,
			PermissionsJson = "[0]",
		}, token: token);

		await context.InsertAsync(new Database.Models.OrganizationApiKey
		{
			OrganizationApiKeyId = keyId,
			PermissionsJson = "[0]",
		}, token: token);
	}

	public async Task<UserId> ReadApiKeyUserIdAsync(string key)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<DbContext>();

		return await context.AccessControl.Users
			.Where(u => u.EmailAddress == key)
			.Select(u => u.UserId)
			.SingleAsync(TestContext.Current.CancellationToken);
	}

	public async Task<(bool HasApiKey, bool IsActive, string EmailAddress)> ReadApiKeyStateAsync(UserId keyId)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<DbContext>();

		var user = await context.AccessControl.Users
			.Where(u => u.UserId == keyId)
			.Select(u => new { HasApiKey = u.ApiKey != null, u.IsActive, u.EmailAddress })
			.SingleAsync(TestContext.Current.CancellationToken);

		return (user.HasApiKey, user.IsActive, user.EmailAddress);
	}

	public HttpClient CreateAnonymousClient()
	{
		return GetFactory()
			.CreateClient(new()
			{
				AllowAutoRedirect = false,
				HandleCookies = true,
			});
	}

	public HttpClient CreateApiKeyClient(string key)
	{
		var client = CreateAnonymousClient();
		client.DefaultRequestHeaders.Add(ApiKeyAuthenticationHandler.HeaderName, key);
		return client;
	}

	public async Task<HttpClient> CreateClientAsync(UserId userId, bool includeAntiforgeryToken = true)
	{
		var client = CreateAnonymousClient();
		client.DefaultRequestHeaders.Add("X-Test-User", userId.Value.ToString(CultureInfo.InvariantCulture));

		if (includeAntiforgeryToken)
		{
			using var response = await client.GetAsync(new Uri("/api/session", UriKind.Relative), TestContext.Current.CancellationToken);
			response.EnsureSuccessStatusCode();
			var session = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
			client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", session.GetProperty("requestToken").GetString());
		}

		return client;
	}

	public async Task<HttpClient> CreateEmailLoginClientAsync()
	{
		var client = CreateAnonymousClient();
		await RefreshAntiforgeryAsync(client);
		return client;
	}

	public static async Task RefreshAntiforgeryAsync(HttpClient client)
	{
		using var response = await client.GetAsync(new Uri("/api/session", UriKind.Relative), TestContext.Current.CancellationToken);
		response.EnsureSuccessStatusCode();
		var session = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
		client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
		client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", session.GetProperty("requestToken").GetString());
	}

	public async Task AgeLoginChallengeAsync(string sessionId, TimeSpan age)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<DbContext>();
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sessionId));
		var createdAt = DateTimeOffset.UtcNow.Subtract(age);

		await db.AccessControl.EmailLoginChallenges
			.Where(c => c.SessionIdHash == hash)
			.Set(c => c.CreatedAt, createdAt)
			.Set(c => c.ExpiresAt, createdAt.AddMinutes(15))
			.UpdateAsync(TestContext.Current.CancellationToken);
	}

	public async Task<bool> UserExistsAsync(string emailAddress)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<DbContext>();

		return await db.AccessControl.Users
			.AnyAsync(u => u.EmailAddress == emailAddress, TestContext.Current.CancellationToken);
	}

	public async Task DeactivateUserAsync(UserId userId)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<DbContext>();

		await db.AccessControl.Users
			.Where(u => u.UserId == userId)
			.Set(u => u.IsActive, value: false)
			.UpdateAsync(TestContext.Current.CancellationToken);
	}

	public async ValueTask DisposeAsync()
	{
		if (_factory is not null)
			await _factory.DisposeAsync();

		if (_externalServer is not null && _externalDatabase is not null)
			await ExecuteDatabaseCommandAsync(_externalServer, _externalDatabase, create: false);
		else
			await _container.DisposeAsync();
	}

	private static async Task ExecuteDatabaseCommandAsync(string connectionString, string name, bool create)
	{
		await using var connection = new SqlConnection(connectionString);
		await connection.OpenAsync(CancellationToken.None);

		await using var command = create
			? new SqlCommand("DECLARE @sql nvarchar(max) = N'create database ' + QUOTENAME(@name); EXEC sp_executesql @sql;", connection)
			: new SqlCommand("DECLARE @sql nvarchar(max) = N'alter database ' + QUOTENAME(@name) + N' set single_user with rollback immediate; drop database ' + QUOTENAME(@name); EXEC sp_executesql @sql;", connection);

		command.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = name;
		await command.ExecuteNonQueryAsync(CancellationToken.None);
	}

	private WebApplicationFactory<DbContext> GetFactory() =>
		_factory ?? throw new InvalidOperationException("The test host has not started.");
}

file sealed class TestWebApplicationFactory(string connectionString, CapturingEmailSender emails) : WebApplicationFactory<DbContext>
{
	protected override IHost CreateHost(IHostBuilder builder)
	{
		builder
			.UseEnvironment("Testing")
			.ConfigureAppConfiguration(cb => cb.AddInMemoryCollection(
				new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
				{
					["DbContextOptions:ConnectionString"] = connectionString,
					["DbContextOptions:ConnectionStringInit"] = connectionString,
					["EmailLoginOptions:RequestsPerMinute"] = "1000",
					["EmailLoginOptions:VerificationsPerMinute"] = "1000",
					["EmailServiceOptions:ServerType"] = "Disabled",
					["EmailServiceOptions:FromEmailAddress"] = "Tests <tests@example.test>",
					["EmailServiceOptions:AdminEmailAddresses:0"] = "admin@example.test",
				}
			));

		return base.CreateHost(builder);
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder) =>
		builder.ConfigureTestServices(services =>
		{
			services.RemoveAll<IEmailSender>();
			services.AddSingleton<IEmailSender>(emails);
			services.RemoveAll<ILoginTokenGenerator>();
			services.AddSingleton<ILoginTokenGenerator, TestLoginTokenGenerator>();
			services.RemoveAll<TimeProvider>();
			services.AddSingleton<TimeProvider, RequestTimeProvider>();

			services
				.AddAuthentication(options =>
				{
					options.DefaultAuthenticateScheme = TestAuthenticationHandler.Scheme;
					options.DefaultChallengeScheme = TestAuthenticationHandler.Scheme;
				})
				.AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.Scheme, _ => { });
		});
}

file sealed class RequestTimeProvider(IHttpContextAccessor httpContextAccessor) : TimeProvider
{
	public override DateTimeOffset GetUtcNow()
	{
		var header = httpContextAccessor.HttpContext?.Request.Headers["X-Test-Utc-Now"].ToString();
		return DateTimeOffset.TryParseExact(header, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var now)
			? now
			: System.GetUtcNow();
	}
}

file sealed class TestAuthenticationHandler(
	IOptionsMonitor<AuthenticationSchemeOptions> options,
	ILoggerFactory logger,
	UrlEncoder encoder
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
	public new const string Scheme = "Test";

	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		if (!Request.Headers.TryGetValue("X-Test-User", out var value)
			|| !int.TryParse(value.ToString(), CultureInfo.InvariantCulture, out var userId))
		{
			return await Context.AuthenticateAsync(ApiKeyAuthenticationHandler.AuthenticationScheme);
		}

		var identity = new ClaimsIdentity(
			[
				new Claim("vsa-id", userId.ToString(CultureInfo.InvariantCulture)),
				new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
				new Claim(ClaimTypes.Name, "Test User"),
				new Claim(ClaimTypes.Email, "user@example.test"),
			],
			Scheme
		);

		return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme));
	}
}

public sealed partial class CapturingEmailSender : IEmailSender
{
	private readonly ConcurrentDictionary<string, EmailMessage> _messages = new(StringComparer.OrdinalIgnoreCase);
	private readonly ConcurrentDictionary<string, byte> _failures = new(StringComparer.OrdinalIgnoreCase);

	public void FailFor(string emailAddress) => _failures[emailAddress] = 0;
	public void ResumeFor(string emailAddress) => _failures.TryRemove(emailAddress, out _);
	public string ReadCode(string emailAddress) => CodeRegex.Match(_messages[emailAddress].Body).Value;

	public Task SendEmail(EmailMessage message, CancellationToken cancellationToken = default) =>
		SendLoginEmail(message, cancellationToken);

	public Task SendLoginEmail(EmailMessage message, CancellationToken cancellationToken = default)
	{
		var email = message.To
			.Single();

		if (_failures.ContainsKey(email))
			throw new InvalidOperationException("Simulated mail delivery failure.");

		_messages[email] = message;
		return Task.CompletedTask;
	}

	[GeneratedRegex(@"\b[0-9]{6}\b", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1_000)]
	private static partial Regex CodeRegex { get; }
}

file sealed class TestLoginTokenGenerator : ILoginTokenGenerator
{
	private int _value = 10_000;

	public string GenerateToken() =>
		Interlocked.Increment(ref _value).ToString("D6", CultureInfo.InvariantCulture);
}
