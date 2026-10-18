using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// `var act = x.Invoking(s => s.M());` (the delegate kept in a local and asserted on later) becomes `Action act = () => x.M();`
// (the type FluentAssertions returned, written out): Invoking only wraps the lambda and runs nothing, so the local holds the same
// behavior, and the Throw/NotThrow patterns then rewrite `act.Should().Throw<T>()` as a plain delegate subject.
public class InvokingLocalPattern : IPattern
{
    // Literal for check-pattern-docs.sh.
    public string Id => "fa.invoking-local";

    public string Description => "var act = x.Invoking(s => s.M()); → Action act = () => x.M(); (then rewritten by the Throw/NotThrow patterns)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<LocalDeclarationStatementSyntax>()
            .Where(s => !s.IsConst && s.Declaration.Variables is [{ Initializer.Value: InvocationExpressionSyntax call }]
                && call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Invoking" or "Awaiting" }
                && model.GetSymbolInfo(call).Symbol is IMethodSymbol { IsExtensionMethod: true } method
                && FluentAssertionsSyntax.IsFluentAssertions(method));

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var statement = (LocalDeclarationStatementSyntax)node;
        VariableDeclaratorSyntax variable = statement.Declaration.Variables[0];
        var call = (InvocationExpressionSyntax)variable.Initializer!.Value;
        if (!ThrowSyntax.TryGetAction(call, model, out ExpressionSyntax action, out _, out string reason))
        {
            return PatternResult.Manual(reason);
        }

        ITypeSymbol type = ((ILocalSymbol)model.GetDeclaredSymbol(variable)!).Type;
        TypeSyntax declared = statement.Declaration.Type.IsVar
            ? SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, statement.SpanStart)).WithTriviaFrom(statement.Declaration.Type)
            : statement.Declaration.Type;
        VariableDeclaratorSyntax rewritten = variable.WithInitializer(variable.Initializer.WithValue(action.WithTriviaFrom(call)));
        return PatternResult.Fixed(statement.WithDeclaration(statement.Declaration.WithType(declared).WithVariables([rewritten])));
    }
}
