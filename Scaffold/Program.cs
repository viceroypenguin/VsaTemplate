using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using DocoptNet;
using Humanizer;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.SchemaProvider;
using Microsoft.Data.SqlClient;
using SuperLinq;
using VsaTemplate.Scaffold;
using VsaTemplate.SourceGen;

static int ShowHelp(string help)
{
	Console.WriteLine(help);
	return 0;
}

static int OnError(string error)
{
	Console.Error.WriteLine(error);
	return 1;
}

return ProgramArguments.CreateParser()
	.Parse(args) switch
{
	IArgumentsResult<ProgramArguments> { Arguments: var arguments } => await Main(arguments),
	IHelpResult { Help: var help } => ShowHelp(help),
	IInputErrorResult { Error: var error } => OnError(error),
	var result => throw new SwitchExpressionException(result),
};

static async Task<int> Main(ProgramArguments args)
{
	try
	{
		var code = await GenerateScaffold(args);

		var file = args.OptOutputFile!;
		_ = Directory.CreateDirectory(Path.GetDirectoryName(file)!);
		await File.WriteAllTextAsync(file, code);

		return 0;
	}
	catch (Exception ex)
	{
		await Console.Error.WriteLineAsync(ex.Message);
		return 1;
	}
}

static async Task<string> GenerateScaffold(ProgramArguments args)
{
	var dbName = "VsaTemplate_Scaffold_" + Guid.NewGuid().ToString().Replace("-", "", StringComparison.Ordinal);
	var cn = Regex.Unescape(args.OptConnectionString!);

	await using var conn = GetSqlServerConnection(cn, "master");
	await CreateDatabase(conn, dbName);
	try
	{
		await LoadMigrationScripts(conn, dbName, args.ArgFile);

		var provider = conn.DataProvider.GetSchemaProvider();
		var schema = provider.GetSchema(
			conn,
			new()
			{
				ExcludedSchemas = ["Hangfire"],
			}
		);

		var entities = schema.Tables
			.Select(t => new DbEntity
			{
				PropertyName = Pluralize(t.TypeName),
				TypeName = t.TypeName,
				TableName = t.TableName!,
				SchemaName = t.SchemaName is "dbo" ? null : t.SchemaName,

				Properties = t.Columns
					.Select(c => new Property
					{
						PropertyName = c.MemberName,
						ColumnName = c.ColumnName,
						TypeName = GetPropertyType(c.DataType, c.Length),
						IsNullable = c.IsNullable,
						DataType = c.ColumnType ?? throw new InvalidOperationException("Unknown column type"),
						ForceNotNull = c.SystemType!.IsClass && !c.IsNullable,
						IsPrimaryKey = c.IsPrimaryKey,
						PrimaryKeyOrder = c.IsPrimaryKey && t.Columns.Any(c => c.PrimaryKeyOrder > 1) ? c.PrimaryKeyOrder : null,
						IsIdentity = c.IsIdentity,
						SkipOnInsert = c.SkipOnInsert,
						SkipOnUpdate = c.SkipOnUpdate,
					})
					.OrderBy(x => x.PropertyName, StringComparer.Ordinal)
					.ToEquatableReadOnlyList(),

				Associations = t.ForeignKeys
					.Select(fk => new Association
					{
						Name = fk.KeyName,
						CanBeNull = fk.CanBeNull,
						ThisKeys = string.Join(",", fk.ThisColumns.Select(c => c.MemberName)),
						OtherKeys = string.Join(",", fk.OtherColumns.Select(c => c.MemberName)),
						OtherName = GetAssociationName(fk),

						OtherType =
							fk.AssociationType == AssociationType.OneToMany
								? $"IEnumerable<{fk.OtherTable.TypeName}>" :
							fk.CanBeNull
								? $"{fk.OtherTable.TypeName}?" :
								fk.OtherTable.TypeName,
					})
					.GroupBy(x => x.OtherName, StringComparer.Ordinal)
					.SelectMany(g =>
						g
							.Index()
							.Select(x => x.Index > 0
								? x.Item with { OtherName = x.Item.OtherName + x.Index }
								: x.Item
							)
					)
					.OrderBy(x => x.OtherName, StringComparer.Ordinal)
					.ToEquatableReadOnlyList(),
			})
			.OrderBy(x => x.TableName, StringComparer.Ordinal)
			.ToList();

		static string GetAssociationName(ForeignKeySchema fk)
		{
			if (fk.AssociationType is AssociationType.OneToMany)
				return Pluralize(fk.OtherTable.TypeName);

			if (fk.KeyName.EndsWith("BackReference", StringComparison.OrdinalIgnoreCase)
				&& fk.OtherColumns is [{ } oCol])
			{
				return oCol.MemberName[..^2];
			}

			if (!fk.KeyName.EndsWith("BackReference", StringComparison.OrdinalIgnoreCase)
				&& fk.ThisColumns is [{ } tCol]
				&& tCol.MemberName[..^2] != fk.ThisTable!.TypeName)
			{
				return tCol.MemberName[..^2];
			}

			return fk.OtherTable.TypeName;
		}

#pragma warning disable CA1869 // Cache and reuse 'JsonSerializerOptions' instances
		return JsonSerializer.Serialize(entities, new JsonSerializerOptions() { WriteIndented = true });
#pragma warning restore CA1869 // Cache and reuse 'JsonSerializerOptions' instances
	}
	finally
	{
		await DropDatabase(conn, dbName);
	}
}

static string Pluralize(string typeName) =>
	string.Concat(
		typeName
			.Segment(char.IsUpper)
			.TagFirstLast((str, _, isLast) =>
				isLast
					? string.Concat(str).Pluralize()
					: string.Concat(str)
			)
	);

#pragma warning disable IDE0072 // Add missing cases
static string GetPropertyType(DataType? dataType, int? length) =>
	dataType switch
	{
		DataType.Char
		or DataType.NChar => length > 1 ? "string" : "char",

		DataType.VarChar
		or DataType.Text
		or DataType.NVarChar
		or DataType.NText => "string",

		DataType.Binary
		or DataType.VarBinary
		or DataType.Blob => "byte[]",

		DataType.Guid => "global::System.Guid",

		DataType.Boolean => "bool",

		DataType.Byte => "byte",
		DataType.SByte => "sbyte",
		DataType.Int16 => "short",
		DataType.Int32 => "int",
		DataType.Int64 => "long",
		DataType.UInt16 => "ushort",
		DataType.UInt32 => "uint",

		DataType.UInt64 => "ulong",
		DataType.Single => "float",
		DataType.Double => "double",

		DataType.Decimal
		or DataType.Money => "decimal",

		DataType.Date => "global::System.DateOnly",
		DataType.Time => "global::System.TimeSpan",

		DataType.DateTime
		or DataType.DateTime2 => "global::System.DateTime",

		DataType.DateTimeOffset => "global::System.DateTimeOffset",

		_ => throw new InvalidOperationException("Unknown data type."),
	};
#pragma warning restore IDE0072 // Add missing cases

static async Task CreateDatabase(DataConnection conn, string database)
{
	Console.WriteLine($"Creating database: {database}");

	_ = await conn.ExecuteAsync($"use master; create database {database};");
}

static async Task LoadMigrationScripts(DataConnection conn, string dbName, IEnumerable<string> sqlFiles)
{
	Console.WriteLine("Running Migration Scripts");
	_ = await conn.ExecuteAsync($"use {dbName};");

	var sqlBlocksRegex = new Regex(@"^go\r?$", RegexOptions.IgnoreCase | RegexOptions.Multiline, TimeSpan.FromSeconds(1));
	foreach (var path in sqlFiles.OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal))
	{
		Console.WriteLine($"Executing Script: {Path.GetFileName(path)}");

		var text = await File.ReadAllTextAsync(path);
		foreach (var b in sqlBlocksRegex.Split(text).Where(s => !string.IsNullOrWhiteSpace(s)))
			_ = await conn.ExecuteAsync(b);
	}
}

static async Task DropDatabase(DataConnection conn, string database)
{
	Console.WriteLine($"Dropping database: {database}");

	_ = await conn.ExecuteAsync($"""
		use master;
		if exists (select * from sys.databases where name = '{database}')
		begin
			alter database {database} set single_user with rollback immediate;
			drop database {database};
		end
		""");
}

static DataConnection GetSqlServerConnection(string connectionString, string database)
{
	var builder = new SqlConnectionStringBuilder(connectionString)
	{
		InitialCatalog = database,
	};

	return SqlServerTools.CreateDataConnection(
		builder.ToString(),
		SqlServerVersion.v2019,
		SqlServerProvider.MicrosoftDataSqlClient);
}
