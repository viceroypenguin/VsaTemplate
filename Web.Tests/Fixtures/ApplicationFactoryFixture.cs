using System.Text.Json;
using Immediate.Cache;
using LinqToDB;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using Testcontainers.MsSql;
using VsaTemplate.Web.Client;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Tests.Fixtures;

[assembly: AssemblyFixture(typeof(ApplicationFactoryFixture))]

namespace VsaTemplate.Web.Tests.Fixtures;

public sealed class ApplicationFactoryFixture : IAsyncLifetime, IAsyncDisposable
{
	private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

	private WebApplicationFactory<Program> _factory = default!;

	public UserId AdminTokenUserId { get; private set; }
	public UserId UserTokenUserId { get; private set; }

	public const string AdminToken = nameof(AdminToken);
	public const string UserToken = nameof(UserToken);

	public async ValueTask InitializeAsync()
	{
		await _container.StartAsync();

		var connectionString = _container.GetConnectionString();
		_factory = new TestWebApplicationFactory(connectionString);

		// ensure server started
		_ = _factory.Server;

		await using var context = _factory.Services.GetRequiredService<DbContext>();

		AdminTokenUserId = await InsertApiKey(context, AdminToken, ["Admin"]);
		UserTokenUserId = await InsertApiKey(context, UserToken, []);
	}

	private static async Task<UserId> InsertApiKey(DbContext context, string tokenName, IReadOnlyList<string> permissions)
	{
		var newUserId = await context.InsertWithInt32IdentityAsync(
			new Database.Models.User()
			{
				Name = $"Api Key For: -1",
				EmailAddress = tokenName,
				IsActive = true,
				Roles = JsonSerializer.Serialize(permissions),
			},
			token: TestContext.Current.CancellationToken
		);

		_ = await context.InsertAsync(
			new Database.Models.ApiKey()
			{
				ApiKeyId = newUserId,
				OwnerUserId = UserId.From(-1),
			},
			token: TestContext.Current.CancellationToken
		);

		return UserId.From(newUserId);
	}

	public async ValueTask DisposeAsync()
	{
		await _factory.DisposeAsync();
		await _container.DisposeAsync();
	}

	private IWebClient GetHttpClient(string token)
	{
		var client = _factory.CreateClient();

		client.DefaultRequestHeaders.Add(
			"X-Api-Key",
			token
		);

		return RestService.For<IWebClient>(client);
	}

	public IWebClient GetAdminClient() =>
		GetHttpClient(AdminToken);

	public IWebClient GetUserClient() =>
		GetHttpClient(UserToken);

	public OwnedScope<DbContext> GetDbContext() =>
		_factory.Services.GetRequiredService<Owned<DbContext>>().GetScope();
}

file sealed class TestWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
	protected override IHost CreateHost(IHostBuilder builder)
	{
		_ = builder.UseEnvironment("Testing");

		_ = builder.ConfigureHostConfiguration(
			cb => cb.AddInMemoryCollection(
				new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
				{
					["UseSecretsJson"] = bool.FalseString,
					["UseAuth0"] = bool.FalseString,
					["UseHttpsRedirection"] = bool.FalseString,
					["ProcessFeatureJob:Enabled"] = bool.FalseString,
					["DbContextOptions:ConnectionString"] = connectionString,
				}
			)
		);

		return base.CreateHost(builder);
	}
}
