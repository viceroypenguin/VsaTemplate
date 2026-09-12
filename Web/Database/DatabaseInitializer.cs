using System.Reflection;
using System.Text.RegularExpressions;
using LinqToDB.Data;

namespace VsaTemplate.Web.Database;

public static partial class DatabaseInitializer
{
	public static void Initialize<TContext>(this TContext context, ILogger logger)
		where TContext : DataConnection, IVersionedDbContext =>
		new Initializer<TContext>(context, logger).Initialize();

	#region Execute Script
	[GeneratedRegex("^go\\r?$", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled, "en-US")]
	private static partial Regex SqlBlockRegex();
	#endregion

	private sealed class Initializer<TContext>(
		TContext context,
		ILogger logger
	) where TContext : DataConnection, IVersionedDbContext
	{
		private static readonly Assembly Assembly = typeof(TContext).Assembly;

		public void Initialize()
		{
			LogInitializingDb(logger);

			context.CommandTimeout = 600;

			EnsureVersionHistoryExists();
			RunChangeScripts();

			LogDbInitialized(logger);
		}

		private void EnsureVersionHistoryExists() =>
			// script does validation; always run script
			ExecuteScript("00.VersionHistory.sql");

		private void RunChangeScripts()
		{
			var scripts = GetEmbeddedScripts();
			var executedScripts = context.GetExecutedScripts();

			var scriptsToRun = scripts
				.Except(executedScripts, StringComparer.OrdinalIgnoreCase)
				.Order(StringComparer.OrdinalIgnoreCase)
				.ToList();

			foreach (var s in scriptsToRun)
			{
				var startTime = DateTimeOffset.Now;

				using (var ts = context.BeginTransaction())
				{
					ExecuteScript(s);
					ts.Commit();
				}

				var endTime = DateTimeOffset.Now;

				context.RecordExecutedScript(
					sqlName: s,
					startTime,
					endTime
				);
			}
		}

		private void ExecuteScript(string scriptName)
		{
			try
			{
				var script = Assembly.GetContent(scriptName);
				foreach (var b in SqlBlockRegex().Split(script).Where(s => !string.IsNullOrWhiteSpace(s)))
					context.Execute(b);

				LogExecutedScript(logger, scriptName);
			}
			catch (Exception ex)
			{
				LogUnableToRunScript(logger, ex, scriptName);
				throw;
			}
		}

		private static List<string> GetEmbeddedScripts()
		{
			var resourcePrefix = typeof(TContext).Namespace + ".Scripts.";

			return Assembly
				.GetManifestResourceNames()
				.Where(s => Path.GetExtension(s).Equals(".sql", StringComparison.OrdinalIgnoreCase))
				.Select(s => s[resourcePrefix.Length..])
				.ToList();
		}
	}

	public static string GetContent(this Assembly assembly, string relativePath)
	{
		using var stream = assembly.GetStream(relativePath);
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	public static Stream GetStream(this Assembly assembly, string resourceName)
	{
		var manifestResourceName = assembly
			.GetManifestResourceNames()
			.FirstOrDefault(x => x.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase));

		if (string.IsNullOrEmpty(manifestResourceName))
			throw new InvalidOperationException($"Did not find required resource ending in '{resourceName}' in assembly '{assembly.GetName().Name}'.");

		var stream = assembly
			.GetManifestResourceStream(manifestResourceName)
			?? throw new InvalidOperationException($"Did not find required resource '{manifestResourceName}' in assembly '{assembly.GetName().Name}'.");

		return stream;
	}

	#region Logging

	[LoggerMessage(Level = LogLevel.Error, Message = "Unable to run script '{ScriptName}'.")]
	private static partial void LogUnableToRunScript(ILogger logger, Exception ex, string scriptName);

	[LoggerMessage(Level = LogLevel.Information, Message = "Initializing Ossm DB")]
	private static partial void LogInitializingDb(ILogger logger);

	[LoggerMessage(Level = LogLevel.Information, Message = "Executed script '{ScriptName}'.")]
	private static partial void LogExecutedScript(ILogger logger, string scriptName);

	[LoggerMessage(Level = LogLevel.Information, Message = "Ossm DB Initialized")]
	private static partial void LogDbInitialized(ILogger logger);

	#endregion
}

public interface IVersionedDbContext
{
	IReadOnlyList<string> GetExecutedScripts();
	void RecordExecutedScript(string sqlName, DateTimeOffset startTimestamp, DateTimeOffset endTimestamp);
}
