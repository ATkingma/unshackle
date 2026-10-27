using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.XunitV3.Patterns;

/// <summary>
/// <c>Record.ExceptionAsync(..)</c> returns <c>Task&lt;Exception&gt;</c> in v2 and <c>ValueTask&lt;Exception&gt;</c> in v3. An awaited call
/// needs nothing; any other call gets <c>.AsTask()</c>, which gives back exactly the v2 type wherever the result goes. A method group
/// (<c>Func&lt;.., Task&lt;Exception&gt;&gt; f = Record.ExceptionAsync</c>) no longer converts and stays Manual.
/// </summary>
public class RecordExceptionAsyncPattern : IPattern
{
    private const string Method = "ExceptionAsync";

    public string Id => "xunit3.record-exception-async";

    public string Description => "Record.ExceptionAsync(..) not awaited → .AsTask() (v3 returns ValueTask)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(root);
        return root.DescendantNodes().OfType<SimpleNameSyntax>()
            .Where(n => n.Identifier.ValueText == Method && !XunitV3Syntax.IsAwaited(n) && IsRecordExceptionAsync(model, n))
            .Select(n => (SyntaxNode?)XunitV3Syntax.InvocationOf(n) ?? n);
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is not InvocationExpressionSyntax call)
        {
            return PatternResult.Manual(XunitV3Reason.UnsupportedShape,
                "Record.ExceptionAsync is used as a method group; v3 returns ValueTask<Exception>, so wrap it in a lambda ending in .AsTask() by hand.");
        }

        InvocationExpressionSyntax asTask = SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression, call.WithoutTrivia(), SyntaxFactory.IdentifierName("AsTask")));
        return PatternResult.Fixed(asTask.WithTriviaFrom(call));
    }

    private static bool IsRecordExceptionAsync(SemanticModel model, SimpleNameSyntax name)
    {
        SymbolInfo info = model.GetSymbolInfo(name);
        ISymbol? symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        return symbol is IMethodSymbol method && method.ContainingType.ToDisplayString() == "Xunit.Record" && XunitV3Assemblies.IsFrom(method, XunitV3Assemblies.Core);
    }
}
