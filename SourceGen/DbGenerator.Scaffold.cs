using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Scriban;
using VsaTemplate.Scaffold;

namespace VsaTemplate.SourceGen;

public sealed partial class DbGenerator
{
	private static ImmutableArray<DbEntity> ParseScaffold(AdditionalText text, CancellationToken token) =>
		JsonSerializer.Deserialize<ImmutableArray<DbEntity>>(text.GetText(token)!.ToString());

	private static void RenderEntity(
		SourceProductionContext spc,
		DbEntity entity,
		EquatableDictionary<(string UnderlyingTypeName, bool IsEnum)> map,
		string rootNamespace,
		EquatableReadOnlyList<string?> schemas,
		Template template
	)
	{
		var properties = entity.Properties.Collection
			.Select(p =>
			{
				var name = p.TypeName;
				var useConverter = false;
				var forceNotNull = p.ForceNotNull;
				if (map.TryGetValue(p.ColumnName, out var n)
					|| (p.ColumnName.EndsWith("Id", StringComparison.Ordinal)
						&& map.FindValue(p.ColumnName, out n))
					)
				{
					name = n.UnderlyingTypeName;
					useConverter = !n.IsEnum;
					forceNotNull = false;
				}

				return new
				{
					TypeName = name,
					p.IsNullable,
					UseConverter = useConverter,
					p.PropertyName,
					p.ColumnName,
					p.DataType,
					ForceNotNull = forceNotNull,
					p.IsPrimaryKey,
					p.PrimaryKeyOrder,
					p.IsIdentity,
					p.SkipOnInsert,
					p.SkipOnUpdate,
				};
			});

		var output = template
			.Render(
				new
				{
					RootNamespace = rootNamespace,
					Schemas = schemas.Collection,
					entity.SchemaName,
					entity.TableName,
					entity.TypeName,
					Properties = properties,
					Associations = entity.Associations.Collection,
				}
			);

		spc.AddSource($"DbContext.Entity.{entity.TypeName}.g.cs", SourceText.From(output, Encoding.UTF8));
	}

	private static void RenderContext(
		SourceProductionContext spc,
		ImmutableArray<(string PropertyName, string TypeName)> context,
		string rootNamespace,
		EquatableReadOnlyList<string?> schemas,
		Template template
	)
	{
		var tables = context
			.Select(x => new { x.PropertyName, x.TypeName })
			.OrderBy(x => x.TypeName, StringComparer.Ordinal);

		var output = template
			.Render(new
			{
				Tables = tables,
				Schemas = schemas.Collection,
				RootNamespace = rootNamespace,
			});

		spc.AddSource($"DbContext.Context.g.cs", SourceText.From(output, Encoding.UTF8));
	}

	private static void RenderSchema(
		SourceProductionContext spc,
		IEnumerable<(string ColumnName, string TypeName, string UnderlyingTypeName, bool IsEnum)> context,
		string rootNamespace,
		EquatableReadOnlyList<string?> schemas,
		Template template
	)
	{
		var types = context
			.Select(x => new { x.UnderlyingTypeName, x.TypeName })
			.OrderBy(x => x.TypeName, StringComparer.Ordinal);

		var output = template.Render(
			new
			{
				Types = types,
				Schemas = schemas.Collection,
				RootNamespace = rootNamespace,
			});

		spc.AddSource($"DbContext.Schema.g.cs", SourceText.From(output, Encoding.UTF8));
	}
}
