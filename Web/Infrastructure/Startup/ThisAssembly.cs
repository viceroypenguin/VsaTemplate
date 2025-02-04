using System.Reflection;

namespace VsaTemplate.Web.Infrastructure.Startup;

public static class ThisAssembly
{
	public static string RevisionId { get; } = GetRevisionId();
	public static string ShortRevisionId { get; } = RevisionId[..7];
	public static string BranchName { get; } = GetBranchName();

	private static string GetRevisionId() =>
		Assembly.GetExecutingAssembly()
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.First(a => a.Key is "SourceRevision")
			.Value!;

	private static string GetBranchName() =>
		Assembly.GetExecutingAssembly()
			.GetCustomAttributes<AssemblyMetadataAttribute>()
			.First(a => a.Key is "SourceBranch")
			.Value!;
}
