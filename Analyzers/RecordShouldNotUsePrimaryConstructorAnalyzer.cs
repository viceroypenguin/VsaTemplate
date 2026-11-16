using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace VsaTemplate.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RecordShouldNotUsePrimaryConstructorAnalyzer : DiagnosticAnalyzer
{
	public static readonly DiagnosticDescriptor RecordShouldNotUsePrimaryConstructor =
		new(
			id: DiagnosticIds.Vsa0002RecordShouldNotUsePrimaryConstructor,
			title: "Records should not use primary constructors",
			messageFormat: "Record `{0}` has parameters and should not",
			category: "VsaTemplate",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Records with primary constructors are difficult to maintain; remove the primary constructor."
		);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[RecordShouldNotUsePrimaryConstructor];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, RecordDeclaration);
	}

	private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
	{
		var node = (RecordDeclarationSyntax)context.Node;

		if (node.ParameterList is null)
			return;

		context.ReportDiagnostic(
			Diagnostic.Create(
				descriptor: RecordShouldNotUsePrimaryConstructor,
				location: node.ParameterList.GetLocation(),
				node.Identifier.Text
			)
		);
	}
}
