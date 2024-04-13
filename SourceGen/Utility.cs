using System.Reflection;
using Microsoft.CodeAnalysis;
using Scriban;

namespace VsaTemplate.SourceGen;

internal static class Utility
{
	public static Template GetScribanTemplate(string templateName)
	{
		using var stream = Assembly
			.GetExecutingAssembly()
			.GetManifestResourceStream(
				typeof(Utility),
				$"{templateName}.sbntxt"
			)!;

		using var reader = new StreamReader(stream);
		return Template.Parse(reader.ReadToEnd());
	}

	public static IncrementalValuesProvider<T> WhereNotNull<T>(
		this IncrementalValuesProvider<T?> provider
	) where T : struct =>
		provider
			.Where(x => x != null)
			.Select((x, _) => x!.Value);
}
