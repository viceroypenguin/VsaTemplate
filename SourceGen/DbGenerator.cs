using Microsoft.CodeAnalysis;

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
			.Select(
				(x, _) => x
					.SelectMany(x => x.ColumnNames.Collection.Select(cn => (ColumnName: cn, x.TypeName, x.IsEnum)))
					.ToDictionary(x => x.ColumnName, x => (x.TypeName, x.IsEnum), StringComparer.Ordinal)
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
			scaffold
				.Select((x, _) => new ContextProperty
				{
					PropertyName = x.PropertyName,
					TypeName = x.TypeName,
					SchemaName = x.SchemaName,
				})
				.Collect()
				.Combine(rootNamespace)
				.Combine(schemas),
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

	private static MappedType? TransformVogenValueObject(
		GeneratorAttributeSyntaxContext context,
		CancellationToken token
	)
	{
		var symbol = context.TargetSymbol;
		var name = symbol.Name;

		var otherNamesAttribute = symbol.GetAttributes()
			.FirstOrDefault(a => a is
			{
				AttributeClass.Name: "AlternativeColumnNamesAttribute",
				ConstructorArguments:
				[
					{ Kind: TypedConstantKind.Array }
				],
			});

		var otherNames = otherNamesAttribute switch
		{
			{ ConstructorArguments: [{ Values: { IsDefaultOrEmpty: false } values }] } =>
				values.Select(v => v.Value).Cast<string>().ToList(),

			_ => [],
		};

		token.ThrowIfCancellationRequested();

		var model = context.SemanticModel;
		if (model.GetDeclaredSymbol(context.TargetNode, token) is not INamedTypeSymbol valueObject)
			return null;

		var underlying = context.Attributes[0].AttributeClass is { TypeArguments: [{ } ul] }
			? ul.ToDisplayString()
			: "int";

		return new()
		{
			ColumnNames = new([name, .. otherNames]),
			TypeName = valueObject.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
			UnderlyingTypeName = underlying,
			IsEnum = false,
		};
	}

	private static MappedType TransformSyncEnum(
		SyncEnum @enum,
		CancellationToken token
	)
	{
		return new()
		{
			ColumnNames = new([@enum.Name + "Id"]),
			TypeName = @enum.TypeName,
			UnderlyingTypeName = "int",
			IsEnum = true,
		};
	}

	private sealed record MappedType
	{
		public required EquatableReadOnlyList<string> ColumnNames { get; init; }
		public required string TypeName { get; init; }
		public required string UnderlyingTypeName { get; init; }
		public required bool IsEnum { get; init; }
	}

	private sealed record ContextProperty
	{
		public required string PropertyName { get; init; }
		public required string? SchemaName { get; init; }
		public required string TypeName { get; init; }
	}
}
