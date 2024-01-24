using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace VsaTemplate.Web.Infrastructure.Authorization;

public static class ResourceRequirement
{
	public static OperationAuthorizationRequirement Read { get; } = new() { Name = "Read" };
	public static OperationAuthorizationRequirement Update { get; } = new() { Name = "Update" };
	public static OperationAuthorizationRequirement Delete { get; } = new() { Name = "Delete" };
}
