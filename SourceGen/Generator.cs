using Microsoft.CodeAnalysis;

namespace VsaTemplate.SourceGen;

[Generator]
public sealed partial class Generator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
			"Attributes.generator.cs", ThisAssembly.Resources.Attributes.Text));

		GenerateSyncEnums(context);
		GenerateConfigureOptions(context);
	}
}
