using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Scriban;

namespace VsaTemplate.SourceGen;

public sealed partial class DbGenerator
{
	private sealed record SyncEnum
	{
		public required bool DeleteUnknown { get; init; }
		public required string Table { get; init; }
		public required string Name { get; init; }
		public required string TypeName { get; init; }
		public required EquatableReadOnlyList<(string, int?)> Values { get; init; }
	}

	private static SyncEnum TransformEnums(
		GeneratorAttributeSyntaxContext context,
		CancellationToken token
	)
	{
		var @enum = (EnumDeclarationSyntax)context.TargetNode;
		var name = @enum.Identifier.Text;

		token.ThrowIfCancellationRequested();

		var model = context.SemanticModel;
		var symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(context.TargetNode, token)!;

		token.ThrowIfCancellationRequested();

		var list = new List<(string name, int? value)>(@enum.Members.Count);
		foreach (var member in @enum.Members)
		{
			if (member.EqualsValue is not { Value: { } ev })
				continue;

			var value = int.Parse(ev.ToString(), CultureInfo.InvariantCulture);
			if (value == 0)
				continue;

			list.Add((member.Identifier.Text, value));
		}

		token.ThrowIfCancellationRequested();

		var attr = context.Attributes[0];

		var deleteUnknown = (bool?)attr.NamedArguments
			.FirstOrDefault(a => a.Key == "DeleteUnknownValues")
			.Value.Value
			?? true;

		var table = (string?)attr.NamedArguments
			.FirstOrDefault(a => a.Key == "Table")
			.Value.Value
			?? name;

		token.ThrowIfCancellationRequested();

		return new SyncEnum
		{
			DeleteUnknown = deleteUnknown,
			Table = table,
			Name = name,
			TypeName = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			Values = new(list),
		};
	}

	private static void RenderEnum(
		SourceProductionContext spc,
		SyncEnum node,
		EquatableDictionary<(string UnderlyingType, bool IsEnum)> map,
		string rootNamespace,
		Template perEnumTemplate
	)
	{
		var type = map.TryGetValue(node.Table + "Id", out var value)
			? value.UnderlyingType
			: "int";

		var output = perEnumTemplate
			.Render(new
			{
				RootNamespace = rootNamespace,
				node.DeleteUnknown,
				node.Table,
				node.Name,
				Type = type,
				values = node.Values.Collection,
			});

		spc.AddSource($"{node.Name}.g.cs", SourceText.From(output, Encoding.UTF8));
	}

	private static void RenderAllEnums(
		SourceProductionContext spc,
		ImmutableArray<string> n,
		string rootNamespace,
		Template allEnumsTemplate
	)
	{
		var output = allEnumsTemplate
			.Render(new
			{
				Names = n,
				RootNamespace = rootNamespace,
			});

		spc.AddSource("SyncAllEnums.g.cs", SourceText.From(output, Encoding.UTF8));
	}
}
