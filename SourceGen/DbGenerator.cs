using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VsaTemplate.SourceGen;

[Generator]
public sealed partial class DbGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var rootNamespace = context.AnalyzerConfigOptionsProvider
			.Select((p, _) =>
				p.GlobalOptions.TryGetValue("build_property.RootNamespace", out var ns)
					? ns
					: ""
			);

		var valueTypes = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"Vogen.ValueObjectAttribute",
				(_, _) => true,
				TransformVogenValueObject
			)
			.WhereNotNull();

		var valueTTypes = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"Vogen.ValueObjectAttribute`1",
				(_, _) => true,
				TransformVogenValueObject
			)
			.WhereNotNull();

		var syncEnums = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				"VsaTemplate.SyncEnumAttribute",
				(_, _) => true,
				TransformEnums
			);

		var enumIds = syncEnums
			.Select(TransformSyncEnum);

		var allTypes = valueTTypes.Collect()
			.Combine(valueTypes.Collect())
			.Combine(enumIds.Collect())
			.SelectMany((x, _) =>
				x.Left.Left
					.Concat(x.Left.Right)
					.Concat(x.Right)
			)
			.Collect();

		var map = allTypes
			.Select((x, _) => x
				.ToDictionary(x => x.ColumnName, x => (x.TypeName, x.IsEnum))
				.ToEquatableDictionary()
			);

		var scaffold = context.AdditionalTextsProvider
			.Where(x => x.Path.EndsWith("Scaffold.json", StringComparison.OrdinalIgnoreCase))
			.SelectMany(ParseScaffold);

		var schemas = scaffold
			.Select((x, _) => x.SchemaName)
			.Where(x => x is not null)
			.Collect()
			.Select((x, _) => x.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToEquatableReadOnlyList());

		var entityTemplate = Utility.GetScribanTemplate("DbScaffold.Entity");
		context.RegisterSourceOutput(
			scaffold.Combine(map).Combine(rootNamespace.Combine(schemas)),
			(spc, entity) => RenderEntity(spc, entity.Left.Left, entity.Left.Right, entity.Right.Left, entity.Right.Right, entityTemplate)
		);

		var contextTemplate = Utility.GetScribanTemplate("DbScaffold.Context");
		context.RegisterSourceOutput(
			scaffold.Select((x, _) => (x.PropertyName, x.TypeName)).Collect().Combine(rootNamespace).Combine(schemas),
			(spc, context) => RenderContext(spc, context.Left.Left, context.Left.Right, context.Right, contextTemplate)
		);

		var schemaTemplate = Utility.GetScribanTemplate("DbScaffold.Schema");
		context.RegisterSourceOutput(
			allTypes.Combine(rootNamespace),
			(spc, types) => RenderSchema(spc, types.Left, types.Right, schemaTemplate)
		);

		var perEnumTemplate = Utility.GetScribanTemplate("PerEnum");
		context.RegisterSourceOutput(
			syncEnums.Combine(map).Combine(rootNamespace),
			action: (spc, x) => RenderEnum(spc, x.Left.Left, x.Left.Right, x.Right, perEnumTemplate));

		var names = syncEnums
			.Select((n, _) => n.Name)
			.Collect();

		var allEnumsTemplate = Utility.GetScribanTemplate("SyncAllEnums");
		context.RegisterSourceOutput(
			names.Combine(rootNamespace),
			action: (spc, n) => RenderAllEnums(spc, n.Left, n.Right, allEnumsTemplate)
		);
	}

	public static (string ColumnName, string TypeName, string UnderlyingTypeName, bool IsEnum)? TransformVogenValueObject(
		GeneratorAttributeSyntaxContext context,
		CancellationToken token
	)
	{
		var name = ((TypeDeclarationSyntax)context.TargetNode).Identifier.Text;

		token.ThrowIfCancellationRequested();

		var model = context.SemanticModel;
		if (model.GetDeclaredSymbol(context.TargetNode, token) is not INamedTypeSymbol valueObject)
			return null;

		token.ThrowIfCancellationRequested();

		var underlying = context.Attributes[0].AttributeClass is { TypeArguments: [{ } ul] }
			? ul.ToDisplayString()
			: "int";

		return (name, valueObject.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), underlying, false);
	}

	private static (string ColumnName, string TypeName, string UnderlyingTypeName, bool IsEnum) TransformSyncEnum(
		SyncEnum @enum,
		CancellationToken token
	) => (
			@enum.Name + "Id",
			@enum.TypeName,
			"int",
			true
		);
}
