using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace VsaTemplate.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NewRecordShouldUseNamedParametersAnalyzer : DiagnosticAnalyzer
{
	public static readonly DiagnosticDescriptor NewRecordShouldUseNamedParameters =
		new(
			id: DiagnosticIds.Vsa0002NewRecordShouldUseNamedParameters,
			title: "Record Constructor should use Named Parameters",
			messageFormat: "Use named arguments for all arguments to a record constructor",
			category: "VsaTemplate",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Use named arguments for record constructors."
		);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		=> [NewRecordShouldUseNamedParameters];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, ObjectCreationExpression, ImplicitObjectCreationExpression);
	}

	private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
	{
		var token = context.CancellationToken;
		token.ThrowIfCancellationRequested();

		var node = (BaseObjectCreationExpressionSyntax)context.Node;
		if (context.SemanticModel.GetTypeInfo(node, context.CancellationToken).Type is not { } type)
			return;

		token.ThrowIfCancellationRequested();

		// Skip non-records.
		if (!type.IsRecord)
			return;

		// We can't use named arguments inside an Expression<>.
		if (context.IsInExpression(node))
			return;

		token.ThrowIfCancellationRequested();

		// Skip constructors with just one argument, or if all arguments are named.
		if (node.ArgumentList is null || node.ArgumentList.Arguments.Count <= 1)
			return;

		foreach (var argument in node.ArgumentList.Arguments.Where(a => a.NameColon is null))
		{
			token.ThrowIfCancellationRequested();

			context.ReportDiagnostic(
				Diagnostic.Create(
					descriptor: NewRecordShouldUseNamedParameters,
					location: argument.GetLocation()
				)
			);
		}
	}
}
