using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DocoptNet;
using LinqToDB;
using LinqToDB.CodeModel;
using LinqToDB.Data;
using LinqToDB.DataModel;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Scaffold;
using LinqToDB.Schema;
using VsaTemplate.Scaffold;
using Microsoft.Data.SqlClient;

static int ShowHelp(string help) { Console.WriteLine(help); return 0; }

static int OnError(string error) { Console.Error.WriteLine(error); return 1; }

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

		var language = LanguageProviders.CSharp;
		var settings = GetScaffoldOptions(args.OptModelNamespace!);

		var legacyProvider = new LegacySchemaProvider(conn, settings.Schema, language);
		var generator = new Scaffolder(language, HumanizerNameConverter.Instance, settings, new Interceptors());
		var dataModel = generator.LoadDataModel(legacyProvider, legacyProvider);
		dataModel.DataContext.Class.Namespace = args.OptContextNamespace;

		var builder = conn.DataProvider.CreateSqlBuilder(conn.MappingSchema, new DataOptions());
		var files = generator.GenerateCodeModel(
			builder,
			dataModel,
			MetadataBuilders.GetMetadataBuilder(generator.Language, MetadataSource.Attributes),
			new ProviderSpecificStructsEqualityFixer(generator.Language));
		return generator.GenerateSourceCode(dataModel, files)[0].Code;
	}
	finally
	{
		await DropDatabase(conn, dbName);
	}
}

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
	foreach (var path in sqlFiles.OrderBy(f => Path.GetFileName(f)))
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

static ScaffoldOptions GetScaffoldOptions(string modelsNamespace)
{
	var settings = ScaffoldOptions.Default();

	settings.Schema.IncludeSchemas = false;
	_ = settings.Schema.Schemas.Add("Hangfire");

	settings.DataModel.HasDefaultConstructor = false;
	settings.DataModel.HasConfigurationConstructor = false;
	settings.DataModel.HasUntypedOptionsConstructor = false;
	settings.DataModel.HasTypedOptionsConstructor = false;
	settings.DataModel.ContextClassName = "DbContext";
	settings.DataModel.GenerateFindExtensions = FindTypes.None;

	settings.CodeGeneration.Namespace = modelsNamespace;
	settings.CodeGeneration.ClassPerFile = false;

	return settings;
}

internal class Interceptors : ScaffoldInterceptors
{
	public override TypeMapping? GetTypeMapping(DatabaseType databaseType, ITypeParser typeParser, TypeMapping? defaultMapping)
	{
		if (databaseType.Name == "date")
			return new(typeParser.Parse<DateOnly>(), DataType.Date);

		return base.GetTypeMapping(databaseType, typeParser, defaultMapping);
	}
}
