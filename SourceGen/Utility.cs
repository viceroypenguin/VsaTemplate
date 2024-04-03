using System.Reflection;

namespace VsaTemplate.SourceGen;

internal static class Utility
{
	public static string GetScribanTemplate(string templateName)
	{
		using var stream = Assembly
			.GetExecutingAssembly()
			.GetManifestResourceStream(
				typeof(Utility),
				$"{templateName}.sbntxt"
			)!;

		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

}
