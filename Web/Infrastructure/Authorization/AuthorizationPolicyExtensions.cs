namespace VsaTemplate.Web.Infrastructure.Authorization;

public static class AuthorizationPolicyExtensions
{
	public static void AddAuthorizationPolicies(this IServiceCollection services) =>
		services.AddAuthorizationBuilder()
			.AddPolicy(Policies.ValidUser, p => p.RequireClaim(Claims.Id))
			.AddPolicy(Policies.Admin, p => p.RequireClaim(Claims.Id).RequireRole(Policies.Admin));
}
