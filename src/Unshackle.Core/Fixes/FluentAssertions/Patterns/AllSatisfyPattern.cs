using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// AllSatisfy takes an Action<T> with assertions, Shouldly's ShouldAllBe a bool predicate: the equivalent is a loop.
public class AllSatisfyPattern : IPattern
{
    public string Id => "fa.all-satisfy";

    public string Description => "x.Should().AllSatisfy(a => ...) → x.ShouldNotBeEmpty(); foreach (var a in x) { ... }";

    // The node is the whole statement (it is replaced by two statements), or the bare invocation when the result is used.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        FluentAssertionsSyntax.FindInvocations(root, model, "AllSatisfy")
            .Select<InvocationExpressionSyntax, SyntaxNode>(i => i.Parent is ExpressionStatementSyntax statement ? statement : i);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (FluentAssertionsSyntax.IsInsideAssertionScope(node, model))
        {
            return PatternResult.Manual(FluentAssertionsSyntax.InsideAssertionScopeReason);
        }

        var statement = node as ExpressionStatementSyntax;
        var assertion = (InvocationExpressionSyntax)(statement?.Expression ?? node);
        ExpressionSyntax? subject = FluentAssertionsSyntax.GetSubject(assertion);
        if (subject is null)
        {
            return PatternResult.Manual("AllSatisfy(...) is not in the form x.Should().AllSatisfy(...), e.g. after .And.");
        }

        if (statement is null)
        {
            return PatternResult.Manual("The result of AllSatisfy(...) is used (.And or a value); a foreach loop returns nothing.");
        }

        if (!FluentAssertionsSyntax.TryGetArguments(assertion, model, out IReadOnlyList<ArgumentSyntax> values, out ArgumentSyntax? because))
        {
            return PatternResult.Manual("AllSatisfy(...) with becauseArgs or named arguments: the message cannot be carried over.");
        }

        if (because is not null)
        {
            return PatternResult.Manual("AllSatisfy(...) with a because message: it cannot be carried into the loop body.");
        }

        if (values.Count != 1 || values[0].Expression is not LambdaExpressionSyntax)
        {
            return PatternResult.Manual("The inspector is not an inline lambda (a method group or a delegate variable).");
        }

        var lambda = (LambdaExpressionSyntax)values[0].Expression;
        (ParameterSyntax? parameter, bool isAsync) = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => (simple.Parameter, simple.AsyncKeyword != default),
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters.Count: 1 } paren => (paren.ParameterList.Parameters[0], paren.AsyncKeyword != default),
            _ => (null, false),
        };
        if (parameter is null || isAsync || parameter.Modifiers.Count > 0)
        {
            return PatternResult.Manual("The inspector lambda is async or does not have exactly one plain parameter.");
        }

        if (lambda.Body is BlockSyntax block && block.DescendantNodes(n => n is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                .Any(n => n is ReturnStatementSyntax))
        {
            return PatternResult.Manual("The inspector lambda contains a return: in a foreach loop it would leave the enclosing method.");
        }

        if (statement.Parent is not (BlockSyntax or SwitchSectionSyntax))
        {
            return PatternResult.Manual("The statement is not directly in a block, so it cannot be split into a check and a loop.");
        }

        if (!IsSimple(subject))
        {
            return PatternResult.Manual("The subject is not a plain name or member access, so the emptiness check and the loop would evaluate it twice.");
        }

        string indent = new([.. statement.GetLeadingTrivia().Reverse().TakeWhile(t => !t.IsKind(SyntaxKind.EndOfLineTrivia)).Reverse().SelectMany(t => t.ToString())]);
        string eol = statement.SyntaxTree.GetText().ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string subjectText = subject.WithoutTrivia().ToString();
        string variable = parameter.Type is null ? $"var {parameter.Identifier.ValueText}" : $"{parameter.Type.WithoutTrivia()} {parameter.Identifier.ValueText}";
        string body = lambda.Body is BlockSyntax inspected
            ? Reindent(inspected.ToString(), inspected, indent, eol)
            : $"{{{eol}{indent}    {lambda.Body.WithoutTrivia()};{eol}{indent}}}";

        StatementSyntax check = SyntaxFactory.ParseStatement($"{subjectText}.ShouldNotBeEmpty();");
        StatementSyntax loop = SyntaxFactory.ParseStatement($"foreach ({variable} in {subjectText}){eol}{indent}{body}");
        return PatternResult.Fixed([check, loop]);
    }

    // Moves the block from the indentation of its closing brace to the indentation of the statement it now belongs to.
    private static string Reindent(string text, BlockSyntax block, string indent, string eol)
    {
        string[] lines = text.ReplaceLineEndings("\n").Split('\n');
        string braceIndent = new([.. block.CloseBraceToken.LeadingTrivia.Reverse().TakeWhile(t => !t.IsKind(SyntaxKind.EndOfLineTrivia)).Reverse().SelectMany(t => t.ToString())]);
        return string.Join(eol, lines.Select((l, i) => i == 0 ? l : indent + (l.StartsWith(braceIndent, StringComparison.Ordinal) ? l[braceIndent.Length..] : l.TrimStart())));
    }

    // Names, `this` and member accesses on those: safe to evaluate twice (shortcut: property getters are assumed pure).
    private static bool IsSimple(ExpressionSyntax e) => e switch
    {
        IdentifierNameSyntax or ThisExpressionSyntax => true,
        MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression, Name: IdentifierNameSyntax } access => IsSimple(access.Expression),
        _ => false,
    };
}
