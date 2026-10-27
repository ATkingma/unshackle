using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.XunitV3;

// Syntax checks shared by the xunit3 patterns.
internal static class XunitV3Syntax
{
    // `await x.M()`, `await x.M().ConfigureAwait(..)` or the same in parentheses: the call's result is only awaited.
    public static bool IsAwaited(SimpleNameSyntax name)
    {
        SyntaxNode callee = name.Parent is MemberAccessExpressionSyntax access && access.Name == name ? access : name;
        if (callee.Parent is not InvocationExpressionSyntax invocation || invocation.Expression != callee)
        {
            return false;
        }

        SyntaxNode result = invocation.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConfigureAwait", Parent: InvocationExpressionSyntax configured }
            ? configured
            : invocation;
        while (result.Parent is ParenthesizedExpressionSyntax parenthesized)
        {
            result = parenthesized;
        }

        return result.Parent is AwaitExpressionSyntax;
    }

    // The invocation that calls the name (`M(..)` or `x.M(..)`), or null for a method group or another use.
    public static InvocationExpressionSyntax? InvocationOf(SimpleNameSyntax name)
    {
        SyntaxNode callee = name.Parent is MemberAccessExpressionSyntax access && access.Name == name ? access : name;
        return callee.Parent is InvocationExpressionSyntax invocation && invocation.Expression == callee ? invocation : null;
    }
}
