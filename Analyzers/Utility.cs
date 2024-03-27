using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;

namespace VsaTemplate.Analyzers;

public static class Utility
{
	public static bool IsInExpression(this SyntaxNodeAnalysisContext context, SyntaxNode node)
	{
		for (var n = node.Parent; n is not (null or MethodDeclarationSyntax); n = n.Parent)
		{
			if (n is not SimpleLambdaExpressionSyntax)
				continue;

			if (context.SemanticModel
					.GetTypeInfo(n, context.CancellationToken)
					.ConvertedType
					.IsExpression())
			{
				return true;
			}
		}

		return false;
	}
}
