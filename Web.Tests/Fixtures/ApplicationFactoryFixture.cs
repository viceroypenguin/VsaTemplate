using System.Text.Json;
using LinqToDB;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using Testcontainers.MsSql;
using TUnit.Core.Interfaces;
using VsaTemplate.Web.Client;
using VsaTemplate.Web.Database;
using VsaTemplate.Web.Features.Users.Models;
using VsaTemplate.Web.Infrastructure.DependencyInjection;

namespace VsaTemplate.Web.Tests.Fixtures;

public sealed class ApplicationFactoryFixture : IAsyncInitializer, IAsyncDisposable
{
	private readonly MsSqlContainer _container;
	private WebApplicationFactory<Program> _factory = default!;

	public const string AdminToken = nameof(AdminToken);
	public const string UserToken = nameof(UserToken);

	public ApplicationFactoryFixture()
	{
		_container = new MsSqlBuilder().Build();
	}

	public async Task InitializeAsync()
	{
		await _container.StartAsync();

		var connectionString = _container.GetConnectionString();
		_factory = new TestWebApplicationFactory(connectionString);

		// ensure server started
		_ = _factory.Server;

		using var context = _factory.Services.GetRequiredService<DbContext>();

		await InsertWebKey(context, AdminToken, ["Admin"]);
		await InsertWebKey(context, UserToken, []);
	}

	private static async Task InsertWebKey(DbContext context, string tokenName, IReadOnlyList<string> permissions)
	{
		var newUserId = await context.InsertWithInt32IdentityAsync(
			new Database.Models.User()
			{
				Name = $"Api Key For: -1",
				EmailAddress = tokenName,
				IsActive = true,
				Roles = JsonSerializer.Serialize(permissions),
			}
		);

		_ = await context.InsertAsync(
			new Database.Models.ApiKey()
			{
				ApiKeyId = newUserId,
				OwnerUserId = UserId.From(-1),
			}
		);
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

	public Owned<DbContext>.IOwnedScope GetDbContext() =>
		_factory.Services.GetRequiredService<Owned<DbContext>>().GetScope();
}

file sealed class TestWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
	protected override IHost CreateHost(IHostBuilder builder)
	{
		_ = builder.UseEnvironment("Testing");

		_ = builder.ConfigureHostConfiguration(
			cb => cb.AddInMemoryCollection(
				new Dictionary<string, string?>
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
