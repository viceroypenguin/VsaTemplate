using System.Reflection;
using System.Text.RegularExpressions;
using LinqToDB;
using LinqToDB.Data;
using VsaTemplate.Database.Models;

namespace VsaTemplate.Database;

public partial class DbContext : DataConnection
{
	public async Task InitializeDatabaseAsync()
	{
		LogInitializingDb();

		CommandTimeout = 600;

		await EnsureVersionHistoryExists();
		await RunChangeScripts();
		await SyncAllEnums();

		s_dbInitialized = true;
		LogDbInitialized();
	}

	#region Main Functions
	private Task EnsureVersionHistoryExists() =>
		// script does validation; always run script
		ExecuteScript("00.VersionHistory.sql");

	private async Task RunChangeScripts()
	{
		var scripts = GetEmbeddedScripts();
		var executedScripts = VersionHistories
			.Select(s => s.SqlFile)
			.ToList();

		var scriptsToRun = scripts
			.Except(executedScripts, StringComparer.OrdinalIgnoreCase)
			.Order()
			.ToList();

		foreach (var s in scriptsToRun)
		{
			var startTime = DateTimeOffset.Now;
			await using (var ts = await BeginTransactionAsync())
			{
				await ExecuteScript(s);
				await ts.CommitAsync();
			}
			var endTime = DateTimeOffset.Now;

			this.Insert(
				new VersionHistory()
				{
					SqlFile = s,
					ExecutionStart = startTime,
					ExecutionEnd = endTime,
				});
		}
	}
	#endregion

	#region Execute Script
	[GeneratedRegex("^go\\r?$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-US")]
	private static partial Regex SqlBlockRegex();

	private async Task ExecuteScript(string scriptName)
	{
		try
		{
			var script = EmbeddedResource.GetContent(scriptName);
			foreach (var b in SqlBlockRegex().Split(script).Where(s => !string.IsNullOrWhiteSpace(s)))
				await this.ExecuteAsync(b);
		}
		catch (Exception ex)
		{
			LogUnableToRunScript(ex, scriptName);
			throw;
		}
	}
	#endregion

	#region Embedded Scripts
	private const string ResourcePrefix = "VsaTemplate.Web.Database.Scripts.";
	private static List<string> GetEmbeddedScripts() =>
		Assembly.GetExecutingAssembly()
			.GetManifestResourceNames()
			.Where(s => Path.GetExtension(s).Equals(".sql", StringComparison.OrdinalIgnoreCase))
			.Select(s => s.Replace(ResourcePrefix, "", StringComparison.OrdinalIgnoreCase))
			.ToList();

	#endregion

	#region Logging

	[LoggerMessage(Level = LogLevel.Error, Message = "Unable to run script '{ScriptName}'.")]
	private partial void LogUnableToRunScript(Exception ex, string scriptName);

	[LoggerMessage(Level = LogLevel.Information, Message = "Initializing VsaTemplate DB")]
	private partial void LogInitializingDb();

	[LoggerMessage(Level = LogLevel.Information, Message = "VsaTemplate DB Initialized")]
	private partial void LogDbInitialized();

	#endregion
}
