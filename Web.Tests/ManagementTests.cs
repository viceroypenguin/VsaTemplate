using System.Globalization;
using System.Net;
using System.Text.Json;
using VsaTemplate.Web.Features.AccessControl.Models;
using VsaTemplate.Web.Tests.Fixtures;

namespace VsaTemplate.Web.Tests;

public sealed class ManagementTests(ApplicationFactoryFixture fixture)
{
	private static readonly string[] ReadOnlyPermissions = ["None"];
	private static readonly string[] DuplicateReadOnlyPermissions = ["None", "None"];
	private static readonly string[] AdministratorPermissions = ["Admin"];
	private static readonly string[] UserManagementPermissions = ["UserManagement"];
	private static readonly string[] OrganizationManagementPermissions = ["OrganizationManagement"];

	[Fact]
	public async Task ReactShellAndAssetsArePublicWhileUnknownApiRoutesStayNotFound()
	{
		using var client = fixture.CreateAnonymousClient();

		using var rootResponse = await client.GetAsync(
			new Uri("/", UriKind.Relative),
			TestContext.Current.CancellationToken
		);

		Assert.Equal(HttpStatusCode.OK, rootResponse.StatusCode);
		Assert.Equal("text/html", rootResponse.Content.Headers.ContentType?.MediaType);
		var html = await rootResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		Assert.Contains("id=\"root\"", html, StringComparison.Ordinal);
		Assert.DoesNotContain("_blazor", html, StringComparison.OrdinalIgnoreCase);

		using var deepLink = await client.GetAsync(
			new Uri("/tenants/123", UriKind.Relative),
			TestContext.Current.CancellationToken
		);

		Assert.Equal(HttpStatusCode.OK, deepLink.StatusCode);
		Assert.Equal(html, await deepLink.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
		await AssertStatusAsync(client, HttpMethod.Get, "/api/does-not-exist", HttpStatusCode.NotFound);

		var scriptTag = html.IndexOf("<script", StringComparison.Ordinal);
		Assert.NotEqual(-1, scriptTag);
		var sourceAttribute = html.IndexOf("src=\"", scriptTag, StringComparison.Ordinal);
		Assert.NotEqual(-1, sourceAttribute);
		var sourceStart = sourceAttribute + "src=\"".Length;
		var sourceEnd = sourceStart + html.AsSpan(sourceStart).IndexOf('"');

		using var script = await client.GetAsync(
			new Uri(html[sourceStart..sourceEnd], UriKind.Relative),
			TestContext.Current.CancellationToken
		);

		Assert.Equal(HttpStatusCode.OK, script.StatusCode);
		Assert.Contains("javascript", script.Content.Headers.ContentType?.MediaType ?? "", StringComparison.Ordinal);
		Assert.NotEmpty(await script.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task AnonymousRequestsCannotReadTenantData()
	{
		using var client = fixture.CreateAnonymousClient();

		using var response = await client.GetAsync(
			new Uri("/api/tenants", UriKind.Relative),
			TestContext.Current.CancellationToken
		);

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task MutationsRequireAnAntiforgeryToken()
	{
		using var client = await fixture.CreateClientAsync(
			await fixture.CreateUserAsync(),
			includeAntiforgeryToken: false
		);

		using var response = await client.PostAsJsonAsync(
			"/api/tenants/create",
			new { name = "Rejected" },
			TestContext.Current.CancellationToken
		);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task AnAntiforgeryTokenCannotBeUsedByAnotherUser()
	{
		using var client = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var otherUser = await fixture.CreateUserAsync();
		client.DefaultRequestHeaders.Remove("X-Test-User");
		client.DefaultRequestHeaders.Add("X-Test-User", otherUser.Value.ToString(CultureInfo.InvariantCulture));

		await AssertStatusAsync(
			client,
			HttpMethod.Post,
			"/api/tenants/create",
			HttpStatusCode.BadRequest,
			new { name = "Rejected" }
		);
	}

	[Fact]
	public async Task ExpiredApiKeysCannotAuthenticate()
	{
		var ownerId = await fixture.CreateUserAsync();
		var key = await fixture.CreateApiKeyAsync(ownerId, DateTimeOffset.UtcNow.AddMinutes(-1));
		using var client = fixture.CreateApiKeyClient(key);
		await AssertStatusAsync(client, HttpMethod.Get, "/api/tenants", HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ApiKeysAuthenticateWithoutAntiforgeryButCannotCreateDelegatedKeys()
	{
		var ownerId = await fixture.CreateUserAsync();
		var key = await fixture.CreateApiKeyAsync(ownerId, DateTimeOffset.UtcNow.AddHours(1));
		using var client = fixture.CreateApiKeyClient(key);
		var session = await GetAsync(client, "/api/session");
		Assert.True(session.GetProperty("isAuthenticated").GetBoolean());

		await AssertStatusAsync(
			client,
			HttpMethod.Post,
			"/api/users/apikey/create",
			HttpStatusCode.Forbidden,
			new { permissions = ReadOnlyPermissions, lifetime = "01:00:00" }
		);
	}

	[Fact]
	public async Task ApiKeyCreationStoresTheRequestedPermissionSubset()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync(isAdministrator: true));

		var result = await PostAsync(
			owner,
			"/api/users/apikey/create",
			new { permissions = ReadOnlyPermissions, lifetime = "01:00:00" }
		);

		var permissions = await fixture.ReadApiKeyPermissionsAsync(result.GetProperty("apiKey").GetString()!);

		Assert.Equal(Permission.None, Assert.Single(permissions));
	}

	[Fact]
	public async Task CachedApiKeysExpireAtTheirOriginalDeadline()
	{
		var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
		var key = await fixture.CreateApiKeyAsync(await fixture.CreateUserAsync(), expiresAt);
		using var client = fixture.CreateApiKeyClient(key);
		Assert.True((await GetAsync(client, "/api/session")).GetProperty("isAuthenticated").GetBoolean());

		client.DefaultRequestHeaders.Add("X-Test-Utc-Now", expiresAt.ToString("O", CultureInfo.InvariantCulture));
		await AssertStatusAsync(client, HttpMethod.Get, "/api/tenants", HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ApiKeyListsContainOnlyOwnedActiveKeysAndNeverReturnFullCredentials()
	{
		var ownerId = await fixture.CreateUserAsync(isAdministrator: true);
		using var owner = await fixture.CreateClientAsync(ownerId);

		var named = await PostAsync(
			owner,
			"/api/users/apikey/create",
			new { permissions = AdministratorPermissions, lifetime = "01:00:00", note = "  Release automation  " }
		);

		var unnamed = await PostAsync(
			owner,
			"/api/users/apikey/create",
			new { permissions = ReadOnlyPermissions, lifetime = "01:00:00" }
		);

		var namedKey = named.GetProperty("apiKey").GetString()!;
		var unnamedKey = unnamed.GetProperty("apiKey").GetString()!;
		var expired = await fixture.CreateApiKeyAsync(ownerId, DateTimeOffset.UtcNow.AddMinutes(-1));

		var unrelated = await fixture.CreateApiKeyAsync(
			await fixture.CreateUserAsync(),
			DateTimeOffset.UtcNow.AddHours(1)
		);

		await AssertStatusAsync(
			owner,
			HttpMethod.Post,
			"/api/users/apikey/create",
			HttpStatusCode.BadRequest,
			new { permissions = ReadOnlyPermissions, lifetime = "01:00:00", note = new string('a', 201) }
		);

		var listing = await GetAsync(owner, "/api/users/apikey");
		var keys = listing.GetProperty("apiKeys").EnumerateArray().ToArray();
		Assert.True(listing.GetProperty("canCreateAdminKey").GetBoolean());
		Assert.Equal(2, keys.Length);

		var namedRow = Assert.Single(keys, k => k.GetProperty("note").GetString() == "Release automation");
		Assert.Equal(namedKey[^3..], namedRow.GetProperty("keySuffix").GetString());
		Assert.Equal("Admin", Assert.Single(Strings(namedRow.GetProperty("permissions"))));

		var unnamedRow = Assert.Single(keys, k => k.GetProperty("note").ValueKind == JsonValueKind.Null);
		Assert.Equal(unnamedKey[^3..], unnamedRow.GetProperty("keySuffix").GetString());
		Assert.Equal("None", Assert.Single(Strings(unnamedRow.GetProperty("permissions"))));

		foreach (var secret in new[] { namedKey, unnamedKey, expired, unrelated })
			Assert.DoesNotContain(secret, listing.GetRawText(), StringComparison.Ordinal);
	}

	[Fact]
	public async Task OnlyOwnersCanInvalidateCachedApiKeys()
	{
		var ownerId = await fixture.CreateUserAsync();
		using var owner = await fixture.CreateClientAsync(ownerId);
		using var ownerWithoutToken = await fixture.CreateClientAsync(ownerId, includeAntiforgeryToken: false);
		using var otherUser = await fixture.CreateClientAsync(await fixture.CreateUserAsync(isAdministrator: true));
		var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
		var key = await fixture.CreateApiKeyAsync(ownerId, expiresAt);
		var retainedKey = await fixture.CreateApiKeyAsync(ownerId, expiresAt);
		var keyId = await fixture.ReadApiKeyUserIdAsync(key);
		await fixture.AddApiKeyScopePermissionsAsync(keyId);
		var path = $"/api/users/apikey/{keyId.Value}";
		using var keyClient = fixture.CreateApiKeyClient(key);
		using var caseVariantClient = fixture.CreateApiKeyClient("T" + key[1..]);

		Assert.True((await GetAsync(keyClient, "/api/session")).GetProperty("isAuthenticated").GetBoolean());
		await AssertStatusAsync(caseVariantClient, HttpMethod.Get, "/api/tenants", HttpStatusCode.Unauthorized);
		await AssertStatusAsync(keyClient, HttpMethod.Get, "/api/users/apikey", HttpStatusCode.Forbidden);
		await AssertStatusAsync(keyClient, HttpMethod.Delete, path, HttpStatusCode.Forbidden);
		await AssertStatusAsync(otherUser, HttpMethod.Delete, path, HttpStatusCode.NotFound);
		await AssertStatusAsync(ownerWithoutToken, HttpMethod.Delete, path, HttpStatusCode.BadRequest);
		// Rejected attempts leave the key valid.
		await AssertStatusAsync(keyClient, HttpMethod.Get, "/api/tenants", HttpStatusCode.OK);

		await AssertStatusAsync(owner, HttpMethod.Delete, path, HttpStatusCode.OK);
		await AssertStatusAsync(owner, HttpMethod.Delete, path, HttpStatusCode.NotFound);
		await AssertStatusAsync(keyClient, HttpMethod.Get, "/api/tenants", HttpStatusCode.Unauthorized);

		var (hasApiKey, isActive, emailAddress) = await fixture.ReadApiKeyStateAsync(keyId);
		Assert.False(hasApiKey);
		Assert.False(isActive);
		Assert.NotEqual(key, emailAddress, StringComparer.Ordinal);

		var remaining = Assert.Single(
			(await GetAsync(owner, "/api/users/apikey")).GetProperty("apiKeys").EnumerateArray()
		);

		Assert.Equal(retainedKey[^3..], remaining.GetProperty("keySuffix").GetString());
		using var retainedKeyClient = fixture.CreateApiKeyClient(retainedKey);
		Assert.True((await GetAsync(retainedKeyClient, "/api/session")).GetProperty("isAuthenticated").GetBoolean());
	}

	[Fact]
	public async Task SyntheticApiKeyUsersCannotBeAddedAsTenantMembersOrExposeTheirSecret()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);
		var roleId = await GetOnlyRoleIdAsync(owner, $"/api/tenants/{tenantId}/roles");

		var created = await PostAsync(
			owner,
			"/api/users/apikey/create",
			new { permissions = ReadOnlyPermissions, lifetime = "01:00:00" }
		);

		var key = created.GetProperty("apiKey").GetString()!;
		var keyUserId = await fixture.ReadApiKeyUserIdAsync(key);

		await AssertStatusAsync(
			owner,
			HttpMethod.Put,
			$"/api/tenants/{tenantId}/roles/{roleId}/users/{keyUserId.Value}",
			HttpStatusCode.NotFound
		);

		var users = await GetAsync(owner, $"/api/tenants/{tenantId}/users");

		Assert.DoesNotContain(
			users.GetProperty("users").EnumerateArray(),
			u => u.GetProperty("userId").GetInt32() == keyUserId.Value
		);

		Assert.DoesNotContain(key, users.GetRawText(), StringComparison.Ordinal);
	}

	[Fact]
	public async Task TenantCreationGrantsOwnershipAndOnlyReturnsRecoveryKeysOnCreation()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		using var outsider = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var created = await PostAsync(owner, "/api/tenants/create", new { name = "  Acme  " });
		var tenantId = created.GetProperty("tenantId").GetInt32();

		var keys = created.GetProperty("recoveryKeys")
			.EnumerateArray()
			.Select(k => k.GetString())
			.ToArray();

		Assert.Equal(12, keys.Length);

		Assert.Equal(
			12,
			keys
				.Distinct(StringComparer.Ordinal)
				.Count()
		);

		Assert.All(keys, key => Assert.False(string.IsNullOrWhiteSpace(key)));

		var listed = await GetAsync(owner, "/api/tenants");

		var tenant = Assert.Single(listed.GetProperty("tenants").EnumerateArray());

		Assert.Equal(tenantId, tenant.GetProperty("tenantId").GetInt32());
		Assert.Equal("Acme", tenant.GetProperty("name").GetString());
		Assert.Contains("Admin", Strings(tenant.GetProperty("permissions")), StringComparer.Ordinal);
		Assert.False(tenant.TryGetProperty("recoveryKeys", out _));
		var others = await GetAsync(outsider, "/api/tenants");
		Assert.Empty(others.GetProperty("tenants").EnumerateArray());
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task TenantNamesMustContainText(string name)
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());

		using var response = await owner.PostAsJsonAsync(
			"/api/tenants/create",
			new { name },
			TestContext.Current.CancellationToken
		);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task TenantNamesCannotExceedDatabaseLimit()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());

		using var response = await owner.PostAsJsonAsync(
			"/api/tenants/create",
			new { name = new string('x', 201) },
			TestContext.Current.CancellationToken
		);

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task TenantOwnersCanRenameDeactivateAndReactivateTheirTenant()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);
		await PutAsync(owner, $"/api/tenants/{tenantId}", new { name = "Renamed", isActive = false });

		var inactive = Assert.Single((await GetAsync(owner, "/api/tenants")).GetProperty("tenants").EnumerateArray());

		Assert.Equal("Renamed", inactive.GetProperty("name").GetString());
		Assert.False(inactive.GetProperty("isActive").GetBoolean());
		await AssertStatusAsync(owner, HttpMethod.Get, $"/api/tenants/{tenantId}", HttpStatusCode.Forbidden);
		await AssertStatusAsync(owner, HttpMethod.Get, $"/api/tenants/{tenantId}/roles", HttpStatusCode.Forbidden);
		await PutAsync(owner, $"/api/tenants/{tenantId}", new { name = "Renamed", isActive = true });

		var active = Assert.Single((await GetAsync(owner, "/api/tenants")).GetProperty("tenants").EnumerateArray());

		Assert.True(active.GetProperty("isActive").GetBoolean());
	}

	[Fact]
	public async Task TenantRoleAssignmentRejectsARoleFromAnotherTenant()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var memberId = await fixture.CreateUserAsync();
		var first = await CreateTenantAsync(owner);
		var second = await CreateTenantAsync(owner);
		var foreignRole = await GetOnlyRoleIdAsync(owner, $"/api/tenants/{second}/roles");

		await AssertStatusAsync(
			owner,
			HttpMethod.Put,
			$"/api/tenants/{first}/roles/{foreignRole}/users/{memberId.Value}",
			HttpStatusCode.NotFound
		);

		var members = await GetAsync(owner, $"/api/tenants/{second}/users");

		Assert.DoesNotContain(
			members.GetProperty("users").EnumerateArray(),
			u => u.GetProperty("userId").GetInt32() == memberId.Value
		);
	}

	[Fact]
	public async Task TenantPermissionChangesTakeEffectOnTheNextRequest()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var memberId = await fixture.CreateUserAsync();
		using var member = await fixture.CreateClientAsync(memberId);
		var tenantId = await CreateTenantAsync(owner);
		var route = $"/api/tenants/{tenantId}";
		await AssertStatusAsync(member, HttpMethod.Get, $"{route}/roles", HttpStatusCode.Forbidden);

		var createdRole = await PutAsync(
			owner,
			$"{route}/roles",
			new { name = "Managers", permissions = UserManagementPermissions }
		);

		var roleId = createdRole.GetProperty("roleId").GetInt32();
		await PutAsync(owner, $"{route}/roles/{roleId}/users/{memberId.Value}");
		await GetAsync(member, $"{route}/roles");

		await PutAsync(
			owner,
			$"{route}/roles/{roleId}",
			new { name = "Organization managers", permissions = OrganizationManagementPermissions }
		);

		await AssertStatusAsync(member, HttpMethod.Get, $"{route}/roles", HttpStatusCode.Forbidden);
		await GetAsync(member, $"{route}/organizations");
		await DeleteAsync(owner, $"{route}/users/{memberId.Value}");
		await AssertStatusAsync(member, HttpMethod.Get, $"{route}/organizations", HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task TenantUserManagersCannotGrantAdministrativePermissions()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var managerId = await fixture.CreateUserAsync();
		using var manager = await fixture.CreateClientAsync(managerId);
		var tenantId = await CreateTenantAsync(owner);
		var route = $"/api/tenants/{tenantId}";
		var ownerRole = await GetOnlyRoleIdAsync(owner, $"{route}/roles");

		var role = await PutAsync(
			owner,
			$"{route}/roles",
			new { name = "Managers", permissions = UserManagementPermissions }
		);

		await PutAsync(owner, $"{route}/roles/{role.GetProperty("roleId").GetInt32()}/users/{managerId.Value}");

		await AssertStatusAsync(
			manager,
			HttpMethod.Put,
			$"{route}/roles",
			HttpStatusCode.Forbidden,
			new { name = "Escalated", permissions = AdministratorPermissions }
		);

		await AssertStatusAsync(
			manager,
			HttpMethod.Put,
			$"{route}/roles/{ownerRole}/users/{managerId.Value}",
			HttpStatusCode.Forbidden
		);
	}

	[Fact]
	public async Task TheLastTenantAdministratorCannotBeRemovedOrDemoted()
	{
		var ownerId = await fixture.CreateUserAsync();
		using var owner = await fixture.CreateClientAsync(ownerId);
		var tenantId = await CreateTenantAsync(owner);
		var route = $"/api/tenants/{tenantId}";
		var roleId = await GetOnlyRoleIdAsync(owner, $"{route}/roles");
		await AssertStatusAsync(owner, HttpMethod.Delete, $"{route}/users/{ownerId.Value}", HttpStatusCode.Conflict);

		await AssertStatusAsync(
			owner,
			HttpMethod.Put,
			$"{route}/roles/{roleId}",
			HttpStatusCode.Conflict,
			new { name = "Former owners", permissions = UserManagementPermissions }
		);

		await GetAsync(owner, $"{route}/roles");
	}

	[Fact]
	public async Task OrganizationsAreScopedToTheirTenantAndOwner()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		using var outsider = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);
		var organizationId = await CreateOrganizationAsync(owner, tenantId);
		var organizations = await GetAsync(owner, $"/api/tenants/{tenantId}/organizations");

		var organization = Assert.Single(organizations.GetProperty("organizations").EnumerateArray());

		Assert.Equal(organizationId, organization.GetProperty("organizationId").GetInt32());
		Assert.Equal(tenantId, organization.GetProperty("tenantId").GetInt32());
		Assert.Contains("Admin", Strings(organization.GetProperty("permissions")), StringComparer.Ordinal);
		var self = await GetAsync(owner, "/api/organizations");

		Assert.Contains(
			self.GetProperty("organizations").EnumerateArray(),
			o => o.GetProperty("organizationId").GetInt32() == organizationId
		);

		await AssertStatusAsync(
			outsider,
			HttpMethod.Get,
			$"/api/tenants/{tenantId}/organizations",
			HttpStatusCode.Forbidden
		);

		await AssertStatusAsync(
			outsider,
			HttpMethod.Put,
			$"/api/tenants/{tenantId}/organizations",
			HttpStatusCode.Forbidden,
			new { name = "Rejected" }
		);

		await AssertStatusAsync(
			outsider,
			HttpMethod.Put,
			$"/api/organizations/{organizationId}",
			HttpStatusCode.Forbidden,
			new { name = "Rejected", isActive = true }
		);

		var outsiderOrganizations = await GetAsync(outsider, "/api/organizations");
		Assert.Empty(outsiderOrganizations.GetProperty("organizations").EnumerateArray());
	}

	[Fact]
	public async Task OrganizationsCanBeRenamedDeactivatedAndReactivated()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);
		var organizationId = await CreateOrganizationAsync(owner, tenantId);
		await PutAsync(owner, $"/api/organizations/{organizationId}", new { name = "Renamed", isActive = false });

		var inactive = Assert.Single(
			(await GetAsync(owner, $"/api/tenants/{tenantId}/organizations")).GetProperty("organizations")
			.EnumerateArray()
		);

		Assert.Equal("Renamed", inactive.GetProperty("name").GetString());
		Assert.False(inactive.GetProperty("isActive").GetBoolean());
		await PutAsync(owner, $"/api/organizations/{organizationId}", new { name = "Renamed", isActive = true });

		var active = Assert.Single(
			(await GetAsync(owner, $"/api/tenants/{tenantId}/organizations")).GetProperty("organizations")
			.EnumerateArray()
		);

		Assert.True(active.GetProperty("isActive").GetBoolean());
	}

	[Fact]
	public async Task OrganizationRolesCannotBeAssignedAcrossOrganizations()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);
		var first = await CreateOrganizationAsync(owner, tenantId);
		var second = await CreateOrganizationAsync(owner, tenantId);
		var foreignRole = await GetOnlyRoleIdAsync(owner, $"/api/organizations/{second}/roles");
		var memberId = await fixture.CreateUserAsync();
		var tenantRole = await GetOnlyRoleIdAsync(owner, $"/api/tenants/{tenantId}/roles");
		await PutAsync(owner, $"/api/tenants/{tenantId}/roles/{tenantRole}/users/{memberId.Value}");

		await AssertStatusAsync(
			owner,
			HttpMethod.Put,
			$"/api/organizations/{first}/roles/{foreignRole}/users/{memberId.Value}",
			HttpStatusCode.NotFound
		);
	}

	[Fact]
	public async Task OrganizationMembersCanAccessTheirOrganizationWithoutTenantMembership()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);
		var organizationId = await CreateOrganizationAsync(owner, tenantId);
		var roleId = await GetOnlyRoleIdAsync(owner, $"/api/organizations/{organizationId}/roles");
		var outsiderId = await fixture.CreateUserAsync();
		await PutAsync(owner, $"/api/organizations/{organizationId}/roles/{roleId}/users/{outsiderId.Value}");
		using var member = await fixture.CreateClientAsync(outsiderId);
		await GetAsync(member, $"/api/organizations/{organizationId}");
		await GetAsync(member, $"/api/organizations/{organizationId}/roles");
		await AssertStatusAsync(member, HttpMethod.Get, $"/api/tenants/{tenantId}", HttpStatusCode.Forbidden);
		Assert.Empty((await GetAsync(member, "/api/tenants")).GetProperty("tenants").EnumerateArray());

		Assert.Contains(
			(await GetAsync(member, "/api/organizations")).GetProperty("organizations").EnumerateArray(),
			o => o.GetProperty("organizationId").GetInt32() == organizationId
		);

		await PutAsync(owner, $"/api/tenants/{tenantId}", new { name = "Inactive tenant", isActive = false });
		await GetAsync(member, $"/api/organizations/{organizationId}/roles");

		Assert.Contains(
			(await GetAsync(member, "/api/organizations")).GetProperty("organizations").EnumerateArray(),
			o => o.GetProperty("organizationId").GetInt32() == organizationId
		);
	}

	[Fact]
	public async Task TheLastOrganizationAdministratorCannotBeRemovedOrDemoted()
	{
		var ownerId = await fixture.CreateUserAsync();
		using var owner = await fixture.CreateClientAsync(ownerId);
		var tenantId = await CreateTenantAsync(owner);
		var organizationId = await CreateOrganizationAsync(owner, tenantId);
		var route = $"/api/organizations/{organizationId}";
		var roleId = await GetOnlyRoleIdAsync(owner, $"{route}/roles");
		await AssertStatusAsync(owner, HttpMethod.Delete, $"{route}/users/{ownerId.Value}", HttpStatusCode.Conflict);

		await AssertStatusAsync(
			owner,
			HttpMethod.Put,
			$"{route}/roles/{roleId}",
			HttpStatusCode.Conflict,
			new { name = "Former administrators", permissions = UserManagementPermissions }
		);
	}

	[Fact]
	public async Task EmailMembershipAndOrganizationPermissionChangesTakeEffectImmediately()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);
		var organizationId = await CreateOrganizationAsync(owner, tenantId);

		var tenantRole = await PutAsync(
			owner,
			$"/api/tenants/{tenantId}/roles",
			new { name = "Members", permissions = ReadOnlyPermissions }
		);

		var email = $"{Guid.NewGuid():N}@example.test";

		var added = await PutAsync(
			owner,
			$"/api/tenants/{tenantId}/users",
			new { emailAddress = email, roleId = tenantRole.GetProperty("roleId").GetInt32() }
		);

		var memberId = added.GetProperty("userId").GetInt32();
		using var member = await fixture.CreateClientAsync(UserId.From(memberId));
		var route = $"/api/organizations/{organizationId}";
		await AssertStatusAsync(member, HttpMethod.Get, $"{route}/roles", HttpStatusCode.Forbidden);

		var role = await PostAsync(
			owner,
			$"{route}/roles/create",
			new { name = "Managers", permissions = UserManagementPermissions }
		);

		var roleId = role.GetProperty("roleId").GetInt32();
		var organizationMember = await PutAsync(owner, $"{route}/users", new { emailAddress = email, roleId });
		Assert.Equal(memberId, organizationMember.GetProperty("userId").GetInt32());
		await GetAsync(member, $"{route}/roles");

		await AssertStatusAsync(
			member,
			HttpMethod.Post,
			$"{route}/roles/create",
			HttpStatusCode.Forbidden,
			new { name = "Escalated", permissions = AdministratorPermissions }
		);

		await PutAsync(owner, $"{route}/roles/{roleId}", new { name = "Readers", permissions = ReadOnlyPermissions });
		await AssertStatusAsync(member, HttpMethod.Get, $"{route}/roles", HttpStatusCode.Forbidden);

		Assert.Contains(
			(await GetAsync(member, "/api/organizations")).GetProperty("organizations").EnumerateArray(),
			o => o.GetProperty("organizationId").GetInt32() == organizationId
		);

		await DeleteAsync(owner, $"{route}/users/{memberId}");
		var self = await GetAsync(member, "/api/organizations");
		Assert.Empty(self.GetProperty("organizations").EnumerateArray());
	}

	[Fact]
	public async Task RemovingATenantMemberPreservesIndependentOrganizationAccess()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var memberId = await fixture.CreateUserAsync();
		using var member = await fixture.CreateClientAsync(memberId);
		var tenantId = await CreateTenantAsync(owner);
		var organizationId = await CreateOrganizationAsync(owner, tenantId);

		var tenantRole = await PutAsync(
			owner,
			$"/api/tenants/{tenantId}/roles",
			new { name = "Members", permissions = ReadOnlyPermissions }
		);

		await PutAsync(
			owner,
			$"/api/tenants/{tenantId}/roles/{tenantRole.GetProperty("roleId").GetInt32()}/users/{memberId.Value}"
		);

		var organizationRole = await PostAsync(
			owner,
			$"/api/organizations/{organizationId}/roles/create",
			new { name = "Managers", permissions = UserManagementPermissions }
		);

		await PutAsync(
			owner,
			$"/api/organizations/{organizationId}/roles/{organizationRole.GetProperty("roleId").GetInt32()}/users/{memberId.Value}"
		);

		await GetAsync(member, $"/api/organizations/{organizationId}/roles");
		await DeleteAsync(owner, $"/api/tenants/{tenantId}/users/{memberId.Value}");
		await GetAsync(member, $"/api/organizations/{organizationId}/roles");

		Assert.Contains(
			(await GetAsync(member, "/api/organizations")).GetProperty("organizations").EnumerateArray(),
			o => o.GetProperty("organizationId").GetInt32() == organizationId
		);

		Assert.Empty((await GetAsync(member, "/api/tenants")).GetProperty("tenants").EnumerateArray());
	}

	[Fact]
	public async Task ReadOnlyTenantMembershipUsesTheDefaultPermissionAndInactiveScopesDenyAccess()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);
		var memberId = await fixture.CreateUserAsync();
		using var member = await fixture.CreateClientAsync(memberId);

		var role = await PutAsync(
			owner,
			$"/api/tenants/{tenantId}/roles",
			new { name = "Readers", permissions = ReadOnlyPermissions }
		);

		await PutAsync(
			owner,
			$"/api/tenants/{tenantId}/roles/{role.GetProperty("roleId").GetInt32()}/users/{memberId.Value}"
		);

		var tenant = await GetAsync(member, $"/api/tenants/{tenantId}");
		Assert.Equal(tenantId, tenant.GetProperty("tenantId").GetInt32());
		await AssertStatusAsync(member, HttpMethod.Get, $"/api/tenants/{tenantId}/roles", HttpStatusCode.Forbidden);
		await PutAsync(owner, $"/api/tenants/{tenantId}", new { name = "Inactive", isActive = false });
		await AssertStatusAsync(member, HttpMethod.Get, $"/api/tenants/{tenantId}", HttpStatusCode.Forbidden);
		Assert.Empty((await GetAsync(member, "/api/tenants")).GetProperty("tenants").EnumerateArray());
		await PutAsync(owner, $"/api/tenants/{tenantId}", new { name = "Restored", isActive = true });
		await GetAsync(member, $"/api/tenants/{tenantId}");
	}

	[Fact]
	public async Task OrganizationEmailMembershipDoesNotRequireAnExistingTenantUser()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);
		var organizationId = await CreateOrganizationAsync(owner, tenantId);

		var role = await PostAsync(
			owner,
			$"/api/organizations/{organizationId}/roles/create",
			new { name = "Readers", permissions = ReadOnlyPermissions }
		);

		var added = await PutAsync(
			owner,
			$"/api/organizations/{organizationId}/users",
			new { emailAddress = $"{Guid.NewGuid():N}@example.test", roleId = role.GetProperty("roleId").GetInt32() }
		);

		using var member = await fixture.CreateClientAsync(UserId.From(added.GetProperty("userId").GetInt32()));
		await GetAsync(member, $"/api/organizations/{organizationId}");
		Assert.Empty((await GetAsync(member, "/api/tenants")).GetProperty("tenants").EnumerateArray());

		Assert.Contains(
			(await GetAsync(member, "/api/organizations")).GetProperty("organizations").EnumerateArray(),
			o => o.GetProperty("organizationId").GetInt32() == organizationId
		);

		await PutAsync(owner, $"/api/organizations/{organizationId}", new { name = "Inactive", isActive = false });

		await AssertStatusAsync(
			member,
			HttpMethod.Get,
			$"/api/organizations/{organizationId}",
			HttpStatusCode.Forbidden
		);

		Assert.Empty((await GetAsync(member, "/api/organizations")).GetProperty("organizations").EnumerateArray());
		await PutAsync(owner, $"/api/organizations/{organizationId}", new { name = "Restored", isActive = true });
		await GetAsync(member, $"/api/organizations/{organizationId}");
	}

	[Fact]
	public async Task TenantNameLengthIsMeasuredAfterTrimming()
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var name = new string('a', 200);
		var created = await PostAsync(owner, "/api/tenants/create", new { name = $"  {name}  " });
		var tenantId = created.GetProperty("tenantId").GetInt32();
		var route = $"/api/tenants/{tenantId}";
		Assert.Equal(name, (await GetAsync(owner, route)).GetProperty("name").GetString());

		await PutAsync(owner, route, new { name = $"  {name}  ", isActive = true });

		await AssertStatusAsync(
			owner,
			HttpMethod.Post,
			"/api/tenants/create",
			HttpStatusCode.BadRequest,
			new { name = name + "a" }
		);

		await AssertStatusAsync(
			owner,
			HttpMethod.Put,
			route,
			HttpStatusCode.BadRequest,
			new { name = name + "a", isActive = true }
		);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RoleInputsAreValidatedAndNormalized(bool organization)
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);

		var route = organization
			? $"/api/organizations/{await CreateOrganizationAsync(owner, tenantId)}"
			: $"/api/tenants/{tenantId}";

		var createRoute = organization ? $"{route}/roles/create" : $"{route}/roles";
		var createMethod = organization ? HttpMethod.Post : HttpMethod.Put;

		var created = await SendAsync(
			owner,
			createMethod,
			createRoute,
			new { name = "  Readers  ", permissions = DuplicateReadOnlyPermissions }
		);

		var roleId = created.GetProperty("roleId").GetInt32();

		var role = (await GetAsync(owner, $"{route}/roles")).GetProperty("roles")
			.EnumerateArray()
			.Single(r => r.GetProperty("roleId").GetInt32() == roleId);

		Assert.Equal("Readers", role.GetProperty("name").GetString());
		Assert.Equal("None", Assert.Single(role.GetProperty("permissions").EnumerateArray()).GetString());

		object[] invalidInputs =
		[
			new { name = "", permissions = ReadOnlyPermissions },
			new { name = "   ", permissions = ReadOnlyPermissions },
			new { name = new string('a', 201), permissions = ReadOnlyPermissions },
			new { name = "Readers", permissions = Array.Empty<string>() },
			new { name = "Readers", permissions = (string[]?)null },
			new { name = "Readers", permissions = new[] { 999 } },
		];

		foreach (var input in invalidInputs)
		{
			await AssertStatusAsync(owner, createMethod, createRoute, HttpStatusCode.BadRequest, input);
			await AssertStatusAsync(owner, HttpMethod.Put, $"{route}/roles/{roleId}", HttpStatusCode.BadRequest, input);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task MemberEmailValidationRejectsDisplayNamesAndMalformedAddresses(bool organization)
	{
		using var owner = await fixture.CreateClientAsync(await fixture.CreateUserAsync());
		var tenantId = await CreateTenantAsync(owner);

		var route = organization
			? $"/api/organizations/{await CreateOrganizationAsync(owner, tenantId)}"
			: $"/api/tenants/{tenantId}";

		var roleId = await GetOnlyRoleIdAsync(owner, $"{route}/roles");

		foreach (var email in new[] { "", "not-an-email", "Display Name <user@example.test>", new string('a', 501) })
		{
			await AssertStatusAsync(
				owner,
				HttpMethod.Put,
				$"{route}/users",
				HttpStatusCode.BadRequest,
				new { emailAddress = email, roleId }
			);
		}
	}

	private static async Task<int> CreateTenantAsync(HttpClient client) =>
		(await PostAsync(client, "/api/tenants/create", new { name = "Test tenant" })).GetProperty("tenantId")
		.GetInt32();

	private static async Task<int> CreateOrganizationAsync(HttpClient client, int tenantId) =>
		(await PutAsync(client, $"/api/tenants/{tenantId}/organizations", new { name = "Test organization" }))
		.GetProperty("organizationId")
		.GetInt32();

	private static async Task<int> GetOnlyRoleIdAsync(HttpClient client, string path) =>
		Assert.Single((await GetAsync(client, path)).GetProperty("roles").EnumerateArray())
			.GetProperty("roleId")
			.GetInt32();

	private static IEnumerable<string?> Strings(JsonElement array) => array.EnumerateArray()
		.Select(e => e.GetString());

	private static async Task<JsonElement> GetAsync(HttpClient client, string path) =>
		await SendAsync(client, HttpMethod.Get, path);

	private static async Task<JsonElement> PostAsync(HttpClient client, string path, object body) =>
		await SendAsync(client, HttpMethod.Post, path, body);

	private static async Task<JsonElement> PutAsync(HttpClient client, string path, object? body = null) =>
		await SendAsync(client, HttpMethod.Put, path, body);

	private static async Task DeleteAsync(HttpClient client, string path) =>
		await SendAsync(client, HttpMethod.Delete, path);

	private static async Task<JsonElement> SendAsync(
		HttpClient client,
		HttpMethod method,
		string path,
		object? body = null
	)
	{
		using var request = new HttpRequestMessage(method, path);

		if (body is not null)
			request.Content = JsonContent.Create(body);

		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
		var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		Assert.True(response.IsSuccessStatusCode, $"{method} {path} returned {(int)response.StatusCode}: {content}");

		if (content.Length == 0)
			return default;

		using var document = JsonDocument.Parse(content);
		return document.RootElement.Clone();
	}

	private static async Task AssertStatusAsync(
		HttpClient client,
		HttpMethod method,
		string path,
		HttpStatusCode status,
		object? body = null
	)
	{
		using var request = new HttpRequestMessage(method, path);

		if (body is not null)
			request.Content = JsonContent.Create(body);

		using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
		Assert.Equal(status, response.StatusCode);
	}
}
