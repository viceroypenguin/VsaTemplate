using Microsoft.CodeAnalysis;

namespace VsaTemplate.Analyzers;

public static class ITypeSymbolExtensions
{
	public static bool IsExpression(this ITypeSymbol? typeSymbol) =>
		typeSymbol is INamedTypeSymbol
		{
			Name: "Expression",
			ContainingNamespace:
			{
				Name: "Expressions",
				ContainingNamespace:
				{
					Name: "Linq",
					ContainingNamespace:
					{
						Name: "System",
						ContainingNamespace.IsGlobalNamespace: true,
					},
				},
			},
		};

	public static bool IsHandler(this ITypeSymbol? typeSymbol) =>
		typeSymbol is
		{
			Name: "HandlerAttribute",
			ContainingNamespace:
			{
				Name: "Shared",
				ContainingNamespace:
				{
					Name: "Handlers",
					ContainingNamespace:
					{
						Name: "Immediate",
						ContainingNamespace.IsGlobalNamespace: true,
					},
				},
			},
		};

	public static bool IsIEnumerableT(this ITypeSymbol? typeSymbol) =>
		typeSymbol is INamedTypeSymbol
		{
			MetadataName: "IEnumerable`1",
			ContainingNamespace:
			{
				Name: "Generic",
				ContainingNamespace:
				{
					Name: "Collections",
					ContainingNamespace:
					{
						Name: "System",
						ContainingNamespace.IsGlobalNamespace: true,
					},
				},
			},
		};

	public static bool IsVsaTemplateEndpoint(this INamespaceSymbol? namespaceSymbol) =>
		namespaceSymbol is
		{
			Name: "Endpoints",
			ContainingNamespace.ContainingNamespace:
			{
				Name: "Features",
				ContainingNamespace:
				{
					Name: "Web",
					ContainingNamespace:
					{
						Name: "VsaTemplate",
						ContainingNamespace.IsGlobalNamespace: true,
					},
				},
			},
		};

	public static bool IsIAuthorizedRequest(this ITypeSymbol? typeSymbol) =>
		typeSymbol is INamedTypeSymbol
		{
			Name: "IAuthorizedRequest",
			ContainingNamespace:
			{
				Name: "Authorization",
				ContainingNamespace:
				{
					Name: "Infrastructure",
					ContainingNamespace:
					{
						Name: "Web",
						ContainingNamespace:
						{
							Name: "VsaTemplate",
							ContainingNamespace.IsGlobalNamespace: true,
						},
					},
				},
			},
		};
}
