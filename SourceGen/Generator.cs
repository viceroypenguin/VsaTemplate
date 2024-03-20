using System.Reflection;
using Microsoft.CodeAnalysis;

namespace VsaTemplate.SourceGen;

[Generator]
public sealed partial class Generator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
			"Attributes.g.cs", GetScribanTemplate("Attributes")));

		GenerateSyncEnums(context);
		GenerateConfigureOptions(context);
	}

	public static string GetScribanTemplate(string templateName)
	{
		using var stream = Assembly
			.GetExecutingAssembly()
			.GetManifestResourceStream(
				typeof(Generator),
				$"{templateName}.sbntxt"
			)!;

		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
