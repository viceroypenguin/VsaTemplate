using System.Text.Json;
using LinqToDB;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Refit;
using Testcontainers.MsSql;
using VsaTemplate.Api.Client;
using VsaTemplate.Api.Database;
using VsaTemplate.Api.Features.Users.Models;
using VsaTemplate.Api.Tests.Fixtures;

[assembly: AssemblyFixture(typeof(ApplicationFactoryFixture))]

namespace VsaTemplate.Api.Tests.Fixtures;

public sealed class ApplicationFactoryFixture : IAsyncLifetime
{
	private readonly MsSqlContainer _container;
	private WebApplicationFactory<Program> _factory = default!;

	public const string AdminToken = nameof(AdminToken);
	public const string UserToken = nameof(UserToken);

	public ApplicationFactoryFixture()
	{
		_container = new MsSqlBuilder().Build();
	}

	public async ValueTask InitializeAsync()
	{
		await _container.StartAsync();

		var connectionString = _container.GetConnectionString();
		_factory = new TestWebApplicationFactory(connectionString);

		// ensure server started
		_ = _factory.Server;

		using var context = _factory.Services.GetRequiredService<DbContext>();

		await InsertApiKey(context, AdminToken, ["Admin"]);
		await InsertApiKey(context, UserToken, []);
	}

	private static async Task InsertApiKey(DbContext context, string tokenName, IReadOnlyList<string> permissions)
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

	private IApiClient GetHttpClient(string token)
	{
		var client = _factory.CreateClient();

		client.DefaultRequestHeaders.Add(
			"X-Api-Key",
			token
		);

		return RestService.For<IApiClient>(client);
	}

	public IApiClient GetAdminClient() =>
		GetHttpClient(AdminToken);

	public IApiClient GetUserClient() =>
		GetHttpClient(UserToken);
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