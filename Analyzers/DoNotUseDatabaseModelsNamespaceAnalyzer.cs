using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VsaTemplate.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DoNotUseDatabaseModelsNamespaceAnalyzer : DiagnosticAnalyzer
{
	public static readonly DiagnosticDescriptor DoNotUseDatabaseModelsNamespace =
		new(
			id: DiagnosticIds.Vsa0003DoNotUseDatabaseModelsNamespace,
			title: "Do not use the `Database.Models` namepace",
			messageFormat: "`using VsaTemplate.{0}.Database.Models;` namepace is forbidden",
			category: "VsaTemplate",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Database entities should only be referenced via Database.Models.Entity."
		);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[DoNotUseDatabaseModelsNamespace];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.UsingDirective);
	}

	private static void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
	{
		var node = (UsingDirectiveSyntax)context.Node;
		if (node.Name is not QualifiedNameSyntax
			{
				Left: QualifiedNameSyntax
				{
					Left: QualifiedNameSyntax
					{
						Left: IdentifierNameSyntax { Identifier.Text: "VsaTemplate" },
						Right: IdentifierNameSyntax { Identifier.Text: var proj and ("Api" or "Web") },
					},
					Right: IdentifierNameSyntax { Identifier.Text: "Database" },
				},
				Right: IdentifierNameSyntax { Identifier.Text: "Models" },
			})
		{
			return;
		}

		context.ReportDiagnostic(
			Diagnostic.Create(
				DoNotUseDatabaseModelsNamespace,
				node.GetLocation(),
				proj
			)
		);
	}
}
