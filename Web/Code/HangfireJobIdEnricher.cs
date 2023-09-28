﻿using CommunityToolkit.Diagnostics;
using Hangfire.Server;
using Serilog.Core;
using Serilog.Events;

namespace VsaTemplate.Web.Code;

public class HangfireJobIdEnricher : ILogEventEnricher, IServerFilter
{
	private static readonly AsyncLocal<string?> s_hangfireJobId = new();

	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		Guard.IsNotNull(logEvent);
		Guard.IsNotNull(propertyFactory);

		var value = s_hangfireJobId.Value;
		if (!string.IsNullOrWhiteSpace(value))
		{
			logEvent.AddPropertyIfAbsent(
				propertyFactory.CreateProperty("HangfireJobId", value));
		}
	}

	public void OnPerforming(PerformingContext filterContext) =>
		s_hangfireJobId.Value = filterContext?.BackgroundJob?.Id;
	public void OnPerformed(PerformedContext filterContext) =>
		s_hangfireJobId.Value = null;
}
