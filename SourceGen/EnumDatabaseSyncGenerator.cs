using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;

namespace VsaTemplate.SourceGen;

public sealed partial class Generator : IIncrementalGenerator
{
	private static void GenerateSyncEnums(IncrementalGeneratorInitializationContext context)
	{
		var nodes = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"SyncEnumAttribute",
				predicate: static (sn, ct) => true,
				transform: static (ctx, ct) =>
				{
					var @enum = (EnumDeclarationSyntax)ctx.TargetNode;
					var name = @enum.Identifier.Text;

					var length = @enum.Members.Count;
					var array = new (string name, int? value)[length - 1];
					var index = 0;
					foreach (var member in @enum.Members)
					{
						if (member.EqualsValue is not { } ev)
							continue;
						var value = int.Parse(ev.Value.ToString(), CultureInfo.InvariantCulture);
						if (value == 0)
							continue;

						array[index++] = (member.Identifier.Text, value);
					}

					if (index < length) Array.Resize(ref array, index);
					var values = new EquatableArray<(string name, int? value)>(array);

					var attr = ctx.Attributes[0];
					var deleteUnknown = (bool?)attr.NamedArguments
						.FirstOrDefault(a => a.Key == "DeleteUnknownValues")
						.Value.Value ?? true;
					var table = (string?)attr.NamedArguments
						.FirstOrDefault(a => a.Key == "Table")
						.Value.Value ?? name;

					return (deleteUnknown, table, name, values);
				});

		var template = Template.Parse(ThisAssembly.Resources.PerEnum.Text);
		context.RegisterSourceOutput(
			nodes,
			action: (spc, n) =>
			{
				var output = template.Render(new { n.deleteUnknown, n.table, n.name, n.values, });
				spc.AddSource($"{n.name}.g.cs", SourceText.From(output, Encoding.UTF8));
			});

		var names = nodes
			.Select((n, _) => n.name)
			.Collect();

		context.RegisterSourceOutput(
			names,
			action: static (spc, n) =>
			{
				var output = Template.Parse(ThisAssembly.Resources.SyncAllEnums.Text)
					.Render(new { names = n, });
				spc.AddSource("SyncAllEnums.g.cs", SourceText.From(output, Encoding.UTF8));
			});
	}
}
