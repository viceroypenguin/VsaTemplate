using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.MsSql;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Infrastructure.Authentication;
using VsaTemplate.Web.Tests.Fixtures;

[assembly: AssemblyFixture(typeof(ApplicationFactoryFixture))]

namespace VsaTemplate.Web.Tests.Fixtures;

public sealed class ApplicationFactoryFixture : IAsyncLifetime
{
	private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest")
		.WithEnvironment("MSSQL_MEMORY_LIMIT_MB", "2048")
		.Build();
	// Any public type in the application assembly can identify its entry point.
	private WebApplicationFactory<DbContext>? _factory;

	public async ValueTask InitializeAsync()
	{
		await _container.StartAsync(TestContext.Current.CancellationToken);
		_factory = new TestWebApplicationFactory(_container.GetConnectionString());
		_factory.StartServer();
	}

	public async Task<UserId> CreateUserAsync(string name = "Test User", bool isAdministrator = false)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<DbContext>();

		var userId = UserId.From(await context.InsertWithInt32IdentityAsync(
			new Database.Models.AccessControl.User
			{
				Name = name,
				EmailAddress = $"{Guid.NewGuid():N}@example.test",
				IsActive = true,
			},
			token: TestContext.Current.CancellationToken
		));

		if (isAdministrator)
			await context.InsertAsync(new Database.Models.AccessControl.RoleUser
			{
				UserId = userId,
				RoleId = RoleId.From(-1),
				EditedUserId = userId,
			}, token: TestContext.Current.CancellationToken);

		return userId;
	}

	public async Task<string> CreateApiKeyAsync(UserId ownerId, DateTimeOffset expiresAt)
	{
		var key = $"test-key-{Guid.NewGuid():N}";
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<DbContext>();

		var keyId = UserId.From(await context.InsertWithInt32IdentityAsync(new Database.Models.AccessControl.User
		{
			Name = "Test API key",
			EmailAddress = key,
			IsActive = true,
		}, token: TestContext.Current.CancellationToken));

		await context.InsertAsync(new Database.Models.AccessControl.ApiKey
		{
			ApiKeyId = keyId,
			OwnerUserId = ownerId,
			CreatedDateTime = expiresAt.AddHours(-1),
			ExpirationDateTime = expiresAt,
			PermissionsJson = "[0]",
		}, token: TestContext.Current.CancellationToken);

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

	public async Task<UserId> ReadApiKeyUserIdAsync(string key)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<DbContext>();

		return await context.AccessControl.Users
			.Where(u => u.EmailAddress == key)
			.Select(u => u.UserId)
			.SingleAsync(TestContext.Current.CancellationToken);

	}

	public async Task ExpireApiKeyAsync(string key)
	{
		await using var scope = GetFactory().Services.CreateAsyncScope();
		var context = scope.ServiceProvider.GetRequiredService<DbContext>();

		await context.AccessControl.ApiKeys
			.Where(k => k.User.EmailAddress == key)
			.Set(k => k.ExpirationDateTime, DateTimeOffset.UtcNow.AddMinutes(-1))
			.UpdateAsync(TestContext.Current.CancellationToken);

	}

	public HttpClient CreateAnonymousClient() => GetFactory().CreateClient(new()
	{
		AllowAutoRedirect = false,
		HandleCookies = true,
	});

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
			client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", session.GetProperty("requestToken").GetString()!);
		}

		return client;
	}

	public async ValueTask DisposeAsync()
	{

		if (_factory is not null)
			await _factory.DisposeAsync();

		await _container.DisposeAsync();
	}

	private WebApplicationFactory<DbContext> GetFactory() =>
		_factory ?? throw new InvalidOperationException("The test host has not started.");
}

file sealed class TestWebApplicationFactory(string connectionString) : WebApplicationFactory<DbContext>
{
	protected override IHost CreateHost(IHostBuilder builder)
	{

		builder
			.UseEnvironment("Testing")
			.ConfigureHostConfiguration(cb => cb.AddInMemoryCollection(
				new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
				{
					["UseAuth0"] = bool.FalseString,
				}
			))
			.ConfigureAppConfiguration(cb => cb.AddInMemoryCollection(
				new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
				{
					["DbContextOptions:ConnectionString"] = connectionString,
					["EmailServiceOptions:ServerType"] = "Disabled",
					["EmailServiceOptions:FromEmailAddress"] = "Tests <tests@example.test>",
					["EmailServiceOptions:AdminEmailAddresses:0"] = "admin@example.test",
				}
			));

		return base.CreateHost(builder);
	}

	protected override void ConfigureWebHost(IWebHostBuilder builder) =>
		builder.ConfigureTestServices(services => services
			.AddAuthentication(options =>
			{
				options.DefaultAuthenticateScheme = TestAuthenticationHandler.Scheme;
				options.DefaultChallengeScheme = TestAuthenticationHandler.Scheme;
			})
			.AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.Scheme, _ => { })
		);
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
