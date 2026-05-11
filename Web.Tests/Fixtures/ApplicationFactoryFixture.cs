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
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Tests.Fixtures;

[assembly: AssemblyFixture(typeof(ApplicationFactoryFixture))]

namespace VsaTemplate.Web.Tests.Fixtures;

public sealed class ApplicationFactoryFixture : IAsyncLifetime, IAsyncDisposable
{
	private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2025-latest").Build();

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

		_factory.StartServer();

		await using var context = _factory.Services.GetRequiredService<DbContext>();

		AdminTokenUserId = await InsertApiKey(context, AdminToken, [Permission.Admin]);
		UserTokenUserId = await InsertApiKey(context, UserToken, []);
	}

	private static async Task<UserId> InsertApiKey(DbContext context, string tokenName, IReadOnlyList<Permission> permissions)
	{
		var newUserId = UserId.From(
			await context.InsertWithInt32IdentityAsync(
				new Database.Models.AccessControl.User()
				{
					Name = "Api Key For: -1",
					EmailAddress = tokenName,
					IsActive = true,
				},
				token: TestContext.Current.CancellationToken
			)
		);

		var newRoleId = RoleId.From(
			await context.InsertWithInt32IdentityAsync(
				new Database.Models.AccessControl.Role()
				{
					Name = "Administrator",
					PermissionsJson = JsonSerializer.Serialize(permissions),
				},
				token: TestContext.Current.CancellationToken
			)
		);

		await context.InsertAsync(
			new Database.Models.AccessControl.RoleUser()
			{
				UserId = newUserId,
				RoleId = newRoleId,
			},
			token: TestContext.Current.CancellationToken
		);

		await context.InsertAsync(
			new Database.Models.AccessControl.ApiKey()
			{
				ApiKeyId = newUserId,
				OwnerUserId = UserId.From(-1),
				PermissionsJson = JsonSerializer.Serialize(permissions),
			},
			token: TestContext.Current.CancellationToken
		);

		return newUserId;
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
		builder
			.UseEnvironment("Testing")
			.ConfigureHostConfiguration(
				cb => cb.AddInMemoryCollection(
					new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
					{
						["UseAuth0"] = bool.FalseString,
						["ProcessFeatureJob:Enabled"] = bool.FalseString,
					}
				)
			)
			.ConfigureAppConfiguration(
				cb => cb.AddInMemoryCollection(
					new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
					{
						["DbContextOptions:ConnectionString"] = connectionString,
					}
				)
			);

		return base.CreateHost(builder);
	}
}
