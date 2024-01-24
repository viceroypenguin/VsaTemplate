using System.Security.Claims;

namespace VsaTemplate.Web.Infrastructure.Authorization;

public static class AuthorizationPolicyExtensions
{
	public static void AddAuthorizationPolicies(this IServiceCollection services) =>
		services.AddAuthorizationBuilder()
			.AddPolicy("ValidUser", p => p.RequireClaim(ClaimTypes.NameIdentifier))
			.AddPolicy(Policies.Admin, p => p.RequireRole(Policies.Admin))
			.AddPolicy(Policies.Features, p => p.RequireRole([Policies.Admin, Policies.Features]))
		;
}
