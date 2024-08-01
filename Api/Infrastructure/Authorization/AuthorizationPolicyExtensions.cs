using System.Security.Claims;

namespace VsaTemplate.Api.Infrastructure.Authorization;

public static class AuthorizationPolicyExtensions
{
	public static void AddAuthorizationPolicies(this IServiceCollection services) =>
		services.AddAuthorizationBuilder()
			.AddPolicy(Policies.ValidUser, p => p.RequireClaim(ClaimTypes.NameIdentifier))
			.AddPolicy(Policies.Admin, p => p.RequireRole(Policies.Admin));
}
