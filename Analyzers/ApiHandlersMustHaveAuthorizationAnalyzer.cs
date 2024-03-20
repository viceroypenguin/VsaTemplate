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
		[
			ApiHandlerMustHaveAuthorization,
		];

	public override void Initialize(AnalysisContext context)
	{
		if (context == null)
			throw new ArgumentNullException(nameof(context));

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSyntaxNodeAction(AnalyzeSyntaxNode, SyntaxKind.MethodDeclaration);
	}

	private void AnalyzeSyntaxNode(SyntaxNodeAnalysisContext context)
	{
		var token = context.CancellationToken;

		var node = (MethodDeclarationSyntax)context.Node;
		if (node.Identifier.Text is not ("Handle" or "HandleAsync"))
			return;

		if (!node.Modifiers.Any(SyntaxKind.StaticKeyword))
			return;

		if (node.Parent is not TypeDeclarationSyntax handlerDeclaration)
			return;

		var handlerSymbol = context.SemanticModel
			.GetDeclaredSymbol(handlerDeclaration, token);

		if (handlerSymbol is null)
			return;

		var @namespace = handlerSymbol.ContainingNamespace.ToString();
		if (!@namespace.StartsWith("VsaTemplate.Web.Features", StringComparison.Ordinal)
			|| !@namespace.EndsWith("Endpoints", StringComparison.Ordinal))
		{
			return;
		}

		token.ThrowIfCancellationRequested();
		var attribute = handlerSymbol.GetAttributes()
			.Where(a => a.ToString() is "Immediate.Handlers.Shared.HandlerAttribute")
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
		if (requestType.Interfaces
				.Any(s => s.ToString() is "VsaTemplate.Web.Infrastructure.Authorization.IAuthorizedRequest"))
		{
			return;
		}

		var declaration = (TypeDeclarationSyntax?)requestType
			.DeclaringSyntaxReferences
			.FirstOrDefault()
			?.GetSyntax(token);
		if (declaration is null)
			return;

		context.ReportDiagnostic(
			Diagnostic.Create(
				ApiHandlerMustHaveAuthorization,
				declaration.Identifier.GetLocation(),
				requestType.Name
			)
		);
	}
}
