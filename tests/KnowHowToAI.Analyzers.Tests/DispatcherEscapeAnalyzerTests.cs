using System.Collections.Immutable;
using KnowHowToAI.Analyzers;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KnowHowToAI.Analyzers.Tests;

[Trait("Category", "Unit")]
public sealed class DispatcherEscapeAnalyzerTests
{
    [Fact]
    public async Task ConfigureAwaitFalse_InComponent_ReportsDiagnosticAtInvocation()
    {
        var diagnostics = await AnalyzeAsync("""
            using Microsoft.AspNetCore.Components;
            using System.Threading.Tasks;

            sealed class TestComponent : ComponentBase
            {
                async Task UpdateAsync() => await Task.Delay(1).ConfigureAwait(false);
            }
            """);

        AssertDiagnostic(diagnostics, 6);
    }

    [Fact]
    public async Task TaskRun_InComponent_ReportsDiagnosticAtInvocation()
    {
        var diagnostics = await AnalyzeAsync("""
            using Microsoft.AspNetCore.Components;
            using System.Threading.Tasks;

            sealed class TestComponent : ComponentBase
            {
                void Update() => Task.Run(static () => { });
            }
            """);

        AssertDiagnostic(diagnostics, 6);
    }

    [Fact]
    public async Task TaskFactoryStartNew_InComponent_ReportsDiagnosticAtInvocation()
    {
        var diagnostics = await AnalyzeAsync("""
            using Microsoft.AspNetCore.Components;
            using System.Threading.Tasks;

            sealed class TestComponent : ComponentBase
            {
                void Update() => new TaskFactory().StartNew(static () => { });
            }
            """);

        AssertDiagnostic(diagnostics, 6);
    }

    [Fact]
    public async Task ContinueWith_InIndirectComponent_ReportsDiagnosticAtInvocation()
    {
        var diagnostics = await AnalyzeAsync("""
            using Microsoft.AspNetCore.Components;
            using System.Threading.Tasks;

            class BaseComponent : ComponentBase { }
            sealed class TestComponent : BaseComponent
            {
                void Update() => Task.CompletedTask.ContinueWith(static _ => { });
            }
            """);

        AssertDiagnostic(diagnostics, 7);
    }

    [Fact]
    public async Task ConfigureAwaitFalse_InAdapter_DoesNotReportDiagnostic()
    {
        var diagnostics = await AnalyzeAsync("""
            using System.Threading.Tasks;

            sealed class MarkdownDownloadAdapter
            {
                async Task DownloadAsync() => await Task.Delay(1).ConfigureAwait(false);
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NormalAwait_InComponent_DoesNotReportDiagnostic()
    {
        var diagnostics = await AnalyzeAsync("""
            using Microsoft.AspNetCore.Components;
            using System.Threading.Tasks;

            sealed class TestComponent : ComponentBase
            {
                async Task UpdateAsync() => await Task.Delay(1);
            }
            """);

        Assert.Empty(diagnostics);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        var compilation = CSharpCompilation.Create(
            "AnalyzerTestAssembly",
            [CSharpSyntaxTree.ParseText(source)],
            TrustedPlatformReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var compilationDiagnostics = compilation.GetDiagnostics();
        Assert.DoesNotContain(compilationDiagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        var compilationWithAnalyzers = compilation.WithAnalyzers(
            [new DispatcherEscapeAnalyzer()],
            new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty));
        return await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    private static void AssertDiagnostic(ImmutableArray<Diagnostic> diagnostics, int line)
    {
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DispatcherEscapeAnalyzer.DiagnosticId, diagnostic.Id);
        Assert.Equal(line, diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1);
    }

    private static readonly ImmutableArray<MetadataReference> TrustedPlatformReferences =
        ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))!
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .Append((MetadataReference)MetadataReference.CreateFromFile(typeof(ComponentBase).Assembly.Location))
        .ToImmutableArray();
}
