using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;

namespace VsaTemplate.SourceGen;

public sealed partial class Generator : IIncrementalGenerator
{
	private static void GenerateConfigureOptions(IncrementalGeneratorInitializationContext context)
	{
		var classes = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"VsaTemplate.ConfigureOptionsAttribute",
				predicate: static (sn, ct) => true,
				transform: static (ctx, ct) =>
				{
					var @class = (ClassDeclarationSyntax)ctx.TargetNode;
					var name = ctx.TargetSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

					var attr = ctx.Attributes[0];
					var sectionPath = (string?)attr.NamedArguments
						.FirstOrDefault(a => a.Key == "SectionName")
						.Value.Value ?? @class.Identifier.Text;

					return new { Name = name, Path = sectionPath, };
				})
			.Collect();

		context.RegisterSourceOutput(
			classes,
			action: (spc, c) =>
			{
				var output = Template.Parse(ThisAssembly.Resources.ConfigureOptions.Text)
					.Render(new { classes = c });
				spc.AddSource($"ConfigureOptions.g.cs", SourceText.From(output, Encoding.UTF8));
			});
	}
}
