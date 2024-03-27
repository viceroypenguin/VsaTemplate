using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace VsaTemplate.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RecordShouldNotBeDeconstructedAnalyzer : DiagnosticAnalyzer
{
	public static readonly DiagnosticDescriptor RecordShouldNotBeDeconstructed =
		new(
			id: DiagnosticIds.Vsa0003RecordShouldNotBeDeconstructed,
			title: "Records should not be deconstructed",
			messageFormat: "Use named properties instead of deconstruction to access record values",
			category: "VsaTemplate",
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			description: "Use named properties instead of deconstruction to access record values."
		);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
		=> [RecordShouldNotBeDeconstructed];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterOperationAction(AnalyzeDeconstructionAssignment, OperationKind.DeconstructionAssignment);
		context.RegisterOperationAction(AnalyzeForeachLoop, OperationKind.Loop);
	}

	private void AnalyzeForeachLoop(OperationAnalysisContext context)
	{
		if (context.Operation is not IForEachLoopOperation
			{
				Locals.Length: > 1,
				Collection: IConversionOperation
				{
					Operand.Type: { } type,
				},
				LoopControlVariable: { } declaration,
			})
		{
			return;
		}

		if (!type.AllInterfaces.Any(i =>
				i.IsIEnumerableT()
				&& i.TypeArguments is [{ IsRecord: true, }]
			))
		{
			return;
		}

		context.ReportDiagnostic(
			Diagnostic.Create(
				descriptor: RecordShouldNotBeDeconstructed,
				location: declaration.Syntax.GetLocation()
			)
		);
	}

	private static void AnalyzeDeconstructionAssignment(OperationAnalysisContext context)
	{
		if (context.Operation is not IDeconstructionAssignmentOperation
			{
				Value.Type.IsRecord: true,
			})
		{
			return;
		}

		context.ReportDiagnostic(
			Diagnostic.Create(
				descriptor: RecordShouldNotBeDeconstructed,
				location: context.Operation.Syntax.GetLocation()
			)
		);
	}
}
