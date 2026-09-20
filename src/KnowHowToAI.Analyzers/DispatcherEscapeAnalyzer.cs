using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace KnowHowToAI.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DispatcherEscapeAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "KHTAI001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Dispatcher-Escape in Blazor-Komponente",
        "Blazor-Komponenten dürfen den Dispatcher nicht über '{0}' verlassen und müssen externe Ereignisse über InvokeAsync zurückführen",
        "Safety",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (!IsComponent(context.ContainingSymbol.ContainingType) || !TryGetEscapePattern(invocation, out var escapePattern))
            return;

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), escapePattern));
    }

    private static bool IsComponent(INamedTypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (current.Name == "ComponentBase" && current.ContainingNamespace.ToDisplayString() == "Microsoft.AspNetCore.Components")
                return true;
        }

        return false;
    }

    private static bool TryGetEscapePattern(IInvocationOperation invocation, out string escapePattern)
    {
        var method = invocation.TargetMethod;
        if (method.Name == "ConfigureAwait" && invocation.Arguments.Any(argument =>
                argument.Value.ConstantValue is { HasValue: true, Value: false }))
        {
            escapePattern = "ConfigureAwait(false)";
            return true;
        }

        if (method.Name == "Run" && IsTaskType(method.ContainingType))
        {
            escapePattern = "Task.Run";
            return true;
        }

        if (method.Name == "StartNew" && IsTaskFactoryType(method.ContainingType))
        {
            escapePattern = "TaskFactory.StartNew";
            return true;
        }

        if (method.Name == "ContinueWith" && IsTaskType(method.ContainingType))
        {
            escapePattern = "ContinueWith";
            return true;
        }

        escapePattern = string.Empty;
        return false;
    }

    private static bool IsTaskType(INamedTypeSymbol? type) =>
        type is not null &&
        type.Name == "Task" &&
        type.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks";

    private static bool IsTaskFactoryType(INamedTypeSymbol? type) =>
        type is not null &&
        type.Name == "TaskFactory" &&
        type.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks";
}
