using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VsaTemplate.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LoggerMessageDiagnosticSuppressor : DiagnosticSuppressor
{
	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions =>
	[
		new(
			id: "CA1848Suppression",
			suppressedDiagnosticId: "CA1848",
			justification: "Simple `string` does not benefit from performance"
		),
	];

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
		foreach (var diagnostic in context.ReportedDiagnostics)
		{
			if (diagnostic.Location.SourceTree?.GetRoot().FindNode(diagnostic.Location.SourceSpan) is not
					InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 })
			{
				continue;
			}

			context.ReportSuppression(
				Suppression.Create(
					SupportedSuppressions[0],
					diagnostic
				)
			);
		}
	}
}
