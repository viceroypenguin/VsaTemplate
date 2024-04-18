using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace VsaTemplate.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ApiHandlersMustHaveAuthorizationAnalyzer : DiagnosticAnalyzer
{
	public static readonly DiagnosticDescriptor ApiHandlerMustHaveAuthorization =
		new(
			id: DiagnosticIds.Vsa0001ApiHandlersMustHaveAuthorization,
			title: "Api Handler Must Have Authorization",
			messageFormat: "Api Handler {0} must implement `IAuthorizedRequest`",
			category: "VsaTemplate",
			defaultSeverity: DiagnosticSeverity.Error,
			isEnabledByDefault: true,
			description: "Api Handler must support authorization by implementing `IAuthorizedRequest`."
		);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		[ApiHandlerMustHaveAuthorization];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();
		context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.MethodDeclaration);
	}

	private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
	{
		var token = context.CancellationToken;
		token.ThrowIfCancellationRequested();

		if (context.Node is not MethodDeclarationSyntax
			{
				Identifier.Text: "Handle" or "HandleAsync",
				Parent: TypeDeclarationSyntax handlerDeclaration,
			} node)
		{
			return;
		}

		if (!node.Modifiers.Any(SyntaxKind.StaticKeyword))
			return;

		var handlerSymbol = context.SemanticModel
			.GetDeclaredSymbol(handlerDeclaration, token);

		token.ThrowIfCancellationRequested();

		if (handlerSymbol is null)
			return;

		if (!handlerSymbol.ContainingNamespace.IsVsaTemplateEndpoint())
			return;

		token.ThrowIfCancellationRequested();

		var attribute = handlerSymbol.GetAttributes()
			.Where(a => a.AttributeClass.IsHandler())
			.FirstOrDefault();

		if (attribute is null)
			return;

		token.ThrowIfCancellationRequested();

		var requestParameter = node.ParameterList.Parameters.FirstOrDefault();
		if (requestParameter is null)
			return;

		var requestType = (ITypeSymbol?)context.SemanticModel
			.GetSymbolInfo(requestParameter.Type!, token)
			.Symbol;
		if (requestType is null)
			return;

		token.ThrowIfCancellationRequested();

		if (requestType.Interfaces.Any(s => s.IsIAuthorizedRequest()))
			return;

		var declaration = (TypeDeclarationSyntax?)requestType
			.DeclaringSyntaxReferences
			.FirstOrDefault()
			?.GetSyntax(token);

		if (declaration is null)
			return;

		token.ThrowIfCancellationRequested();

		context.ReportDiagnostic(
			Diagnostic.Create(
				ApiHandlerMustHaveAuthorization,
				declaration.Identifier.GetLocation(),
				requestType.Name
			)
		);
	}
}
