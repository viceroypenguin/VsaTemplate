using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VsaTemplate.SourceGen;

[Generator]
public sealed partial class DbGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
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
			.Combine(enumIds.Collect());

		var map = allTypes
			.Select((x, _) => x.Left.Left
				.Concat(x.Left.Right)
				.Concat(x.Right)
				.ToDictionary(x => x.ColumnName, x => (x.TypeName, x.IsEnum))
				.ToEquatableDictionary()
			);

		var scaffold = context.AdditionalTextsProvider
			.Where(x => x.Path.EndsWith("Scaffold.json", StringComparison.OrdinalIgnoreCase))
			.SelectMany(ParseScaffold);

		var entityTemplate = Utility.GetScribanTemplate("DbScaffold.Entity");
		context.RegisterSourceOutput(
			scaffold.Combine(map),
			(spc, entity) => RenderEntity(spc, entity.Left, entity.Right, entityTemplate)
		);

		var contextTemplate = Utility.GetScribanTemplate("DbScaffold.Context");
		context.RegisterSourceOutput(
			scaffold.Select((x, _) => (x.PropertyName, x.TypeName)).Collect(),
			(spc, context) => RenderContext(spc, context, contextTemplate)
		);

		var schemaTemplate = Utility.GetScribanTemplate("DbScaffold.Schema");
		context.RegisterSourceOutput(
			allTypes,
			(spc, types) => RenderSchema(spc, types.Left.Left.Concat(types.Left.Right).Concat(types.Right), schemaTemplate)
		);

		var perEnumTemplate = Utility.GetScribanTemplate("PerEnum");
		context.RegisterSourceOutput(
			syncEnums.Combine(map),
				action: (spc, x) => RenderEnum(spc, x.Left, x.Right, perEnumTemplate));

		var names = syncEnums
			.Select((n, _) => n.Name)
			.Collect();

		var allEnumsTemplate = Utility.GetScribanTemplate("SyncAllEnums");
		context.RegisterSourceOutput(
			names,
			action: (spc, n) => RenderAllEnums(spc, n, allEnumsTemplate)
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
