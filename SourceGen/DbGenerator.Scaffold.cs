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

	private static void RenderEntity(SourceProductionContext spc, DbEntity entity, EquatableDictionary<string> map, Template template)
	{
		var properties = entity.Properties.Collection
			.Select(p => p with
			{
				TypeName =
					map.TryGetValue(p.ColumnName, out var name)
						? name :
					p.ColumnName.EndsWith("Id", StringComparison.Ordinal)
					&& map.FindValue(p.ColumnName, out name)
						? name :
						p.TypeName
			});

		var output = template
			.Render(
				new
				{
					entity.SchemaName,
					entity.TableName,
					entity.TypeName,
					Properties = properties,
					Associations = entity.Associations.Collection,
				}
			);

		spc.AddSource($"DbContext.Entity.{entity.TypeName}.g.cs", SourceText.From(output, Encoding.UTF8));
	}

	private static void RenderContext(SourceProductionContext spc, ImmutableArray<(string PropertyName, string TypeName)> context, Template template)
	{
		var tables = context.Select(x => new { x.PropertyName, x.TypeName, }).OrderBy(x => x.TypeName);
		var output = template.Render(new { Tables = tables, });
		spc.AddSource($"DbContext.Context.g.cs", SourceText.From(output, Encoding.UTF8));
	}

	private static void RenderSchema(SourceProductionContext spc, IEnumerable<(string ColumnName, string TypeName, string UnderlyingTypeName)> context, Template template)
	{
		var types = context.Select(x => new { x.UnderlyingTypeName, x.TypeName, }).OrderBy(x => x.TypeName);
		var output = template.Render(new { Types = types, });
		spc.AddSource($"DbContext.Schema.g.cs", SourceText.From(output, Encoding.UTF8));
	}
}
