using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

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
			if (diagnostic.Location.SourceTree?.GetRoot(context.CancellationToken)
					.FindNode(diagnostic.Location.SourceSpan) is not
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
