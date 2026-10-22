using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Unshackle.Tests.Core.EPPlus;

// One snippet of the cell-compare harness (council round 8, E1): compiled in memory and run. The snippet declares exactly one
// public static method Write(string path) that builds a workbook and saves it to path.
public static class WorkbookProgram
{
    public const string EntryPoint = "Write";

    public static void Run(string code, IEnumerable<MetadataReference> references, string path)
    {
        var compilation = CSharpCompilation.Create(
            "Snippet" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(code)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
        if (!result.Success)
        {
            throw new InvalidOperationException("The snippet does not compile: "
                + string.Join("; ", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        }

        // EPPlus and ClosedXML are already loaded in the test process; the snippet binds to those.
        var assembly = Assembly.Load(stream.ToArray());
        MethodInfo[] writers = [.. assembly.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(m => m.Name == EntryPoint && m.GetParameters() is [{ ParameterType: var p }] && p == typeof(string))];
        if (writers.Length != 1)
        {
            throw new InvalidOperationException($"The snippet must declare exactly one public static {EntryPoint}(string path); found {writers.Length}.");
        }

        try
        {
            writers[0].Invoke(null, [path]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw new InvalidOperationException($"The snippet threw: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}", ex.InnerException);
        }
    }

    public static bool HasEntryPoint(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
            .Any(m => m.Identifier.ValueText == EntryPoint && m.ParameterList.Parameters.Count == 1 && m.Modifiers.Any(SyntaxKind.StaticKeyword) && m.Modifiers.Any(SyntaxKind.PublicKeyword));
}
