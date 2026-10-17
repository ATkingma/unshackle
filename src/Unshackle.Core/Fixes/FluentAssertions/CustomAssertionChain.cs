using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.FluentAssertions;

// `x.Should().A(...)` or `x.Should().A(...).And.B(...)` on a custom assertions class: the Should() call, the subject and the
// method calls in source order. Each `.And` must continue on the same assertions object (AndConstraint<itself>), not on the
// subject (AndConstraint<subject>), or splitting the chain into statements would change what the later links are called on.
public class CustomAssertionChain
{
    public required InvocationExpressionSyntax Should { get; init; }

    public required ExpressionSyntax Subject { get; init; }

    public required IReadOnlyList<InvocationExpressionSyntax> Links { get; init; }

    public bool HasAnd => Links.Count > 1;

    public InvocationExpressionSyntax Top => Links[^1];

    // The statement the whole chain is, else null (a value, an argument, a lambda body).
    public ExpressionStatementSyntax? Statement => Top.Parent as ExpressionStatementSyntax;

    // Null (and chain set) when should is the `x.Should()` of such a chain; else the reason it is not.
    public static string? TryParse(InvocationExpressionSyntax should, SemanticModel model, [NotNullWhen(true)] out CustomAssertionChain? chain)
    {
        chain = null;
        if (should.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Should" } access || should.ArgumentList.Arguments.Count != 0)
        {
            return "Should() is not called as x.Should()";
        }

        if (should.Parent is not MemberAccessExpressionSyntax method || method.Expression != should
            || method.Parent is not InvocationExpressionSyntax first || first.Expression != method)
        {
            return "x.Should() is not followed by a method call (a variable, a property or a method group)";
        }

        ITypeSymbol? shouldType = model.GetTypeInfo(should).Type;
        var links = new List<InvocationExpressionSyntax> { first };
        while (true)
        {
            InvocationExpressionSyntax current = links[^1];
            if (current.Parent is not MemberAccessExpressionSyntax next || next.Expression != current)
            {
                break;
            }

            if (next.Name.Identifier.ValueText != "And")
            {
                return $"the chain continues with .{next.Name.Identifier.ValueText}";
            }

            if (next.Parent is not MemberAccessExpressionSyntax andMethod || andMethod.Expression != next
                || andMethod.Parent is not InvocationExpressionSyntax andCall || andCall.Expression != andMethod)
            {
                return "an .And is not followed by a method call";
            }

            if (!SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(next).Type, shouldType))
            {
                return "an .And continues on the subject, not on the assertions";
            }

            links.Add(andCall);
        }

        chain = new CustomAssertionChain { Should = should, Subject = access.Expression, Links = links };
        return null;
    }
}
