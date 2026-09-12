using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Design", "CA1050:Declare types in namespaces", Justification = "Should be available to all consumers")]
internal static class ThisAssemblyExtensions
{
	extension(ThisAssembly)
	{
		public static string RevisionId => ThisAssembly.InformationalVersion.Split('+', 2)[^1];

		public static string ShortRevisionId => ThisAssembly.RevisionId[..8];
	}
}
