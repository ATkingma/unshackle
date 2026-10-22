using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// `m.SetupSequence(x => x.M(..)).Returns(a).ReturnsAsync(b);` → `m.Object.M(..).Returns(a, Task.FromResult<R>(b));` (council round 8, M2;
// moq-queue.md row 11). Measured (Moq 4.18.4, NSubstitute 6.2.0): the values are answered in order in both; after the last one Moq answers its
// default (0, null; for a Task member a null task) and NSubstitute repeats the last value, so every rewrite carries a warning (Warning).
// Throws, ThrowsAsync, Pass, CallBase and functions as a step stay Manual; another setup of the same member too (M6).
public static class SetupSequenceSyntax
{
    public const string Warning = "SetupSequence: after the last value Moq answers its default, NSubstitute repeats the last value";

    private static readonly string[] s_steps = ["Returns", "ReturnsAsync", "Throws", "ThrowsAsync", "Pass", "CallBase"];

    public static bool IsSequence(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "SetupSequence" }
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Name: "SetupSequence" } method && MoqSyntax.IsMockOfT(method.ContainingType);

    // The statement the steps end in, or null when the chain ends in something else.
    public static ExpressionStatementSyntax? Statement(InvocationExpressionSyntax sequence) => Steps(sequence).Last().Parent as ExpressionStatementSyntax;

    public static MockCall Plan(InvocationExpressionSyntax sequence, SemanticModel model)
    {
        List<InvocationExpressionSyntax> chain = Steps(sequence);
        if (chain.Last().Parent is not ExpressionStatementSyntax statement || chain.Count == 1)
        {
            return MockCall.Blocked(MoqReason.SetupShape, "a SetupSequence that is not a statement with at least one step");
        }

        if (sequence.ArgumentList.Arguments is not [var lambda] || sequence.Expression is not MemberAccessExpressionSyntax { Expression: var receiver })
        {
            return MockCall.Blocked(MoqReason.SetupShape, "a SetupSequence without a single lambda argument");
        }

        if (SetupReturnsSyntax.HasOtherSetup(sequence, model))
        {
            return MockCall.Blocked(MoqReason.SetupsOverlap, "SetupSequence on a member with another setup: NSubstitute's configuration call would take a step of an earlier sequence");
        }

        ExpressionSyntax objectAccess = SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression, receiver.WithoutTrivia(), SyntaxFactory.IdentifierName("Object"));
        MockCall call = MockCall.Rewrite(lambda.Expression, objectAccess, model);
        if (call.Blocker is not null)
        {
            return call;
        }

        if (call.Type is null || call.Type.SpecialType == SpecialType.System_Void)
        {
            return MockCall.Blocked(MoqReason.SequenceStep, "a SetupSequence of a void member (Pass/Throws steps) has no NSubstitute form");
        }

        var values = new List<string>();
        bool needsTasks = false;
        foreach (InvocationExpressionSyntax step in chain.Skip(1))
        {
            string name = ((MemberAccessExpressionSyntax)step.Expression).Name.Identifier.Text;
            if (name is not ("Returns" or "ReturnsAsync"))
            {
                return MockCall.Blocked(MoqReason.SequenceStep, $"a {name} step: NSubstitute's Returns sequence answers values only (council round 8, M2)");
            }

            if (step.ArgumentList.Arguments is not [{ Expression: var value }]
                || model.GetSymbolInfo(step).Symbol is not IMethodSymbol { Parameters: [var parameter] })
            {
                return MockCall.Blocked(MoqReason.SequenceStep, $"a {name} step with several arguments");
            }

            if (value is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression })
            {
                return MockCall.Blocked(MoqReason.SequenceStep, "a null step: NSubstitute's Returns(a, null) overloads are ambiguous with a function");
            }

            bool function = (parameter.Type.TypeKind == TypeKind.Delegate || parameter.Type.SpecialType == SpecialType.System_Delegate)
                && !SymbolEqualityComparer.Default.Equals(parameter.Type, call.Type);
            if (function || value is AnonymousFunctionExpressionSyntax)
            {
                return MockCall.Blocked(MoqReason.SequenceStep, $"a {name} step with a function: a sequence of functions is not rewritten");
            }

            string text = value.WithoutTrivia().ToString();
            if (name == "ReturnsAsync")
            {
                if (call.Type is not INamedTypeSymbol { TypeArguments: [var result] } task
                    || task.OriginalDefinition.ToDisplayString() is not ("System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.ValueTask<TResult>"))
                {
                    return MockCall.Blocked(MoqReason.SequenceStep, "ReturnsAsync on a member that is not Task<T>/ValueTask<T>");
                }

                string resultType = result.ToMinimalDisplayString(model, sequence.SpanStart);
                text = task.Name == "Task" ? $"Task.FromResult<{resultType}>({text})" : $"new ValueTask<{resultType}>({text})";
                needsTasks = true;
            }

            values.Add(text);
        }

        StatementSyntax rewritten = SyntaxFactory.ParseStatement($"{call.Node}.Returns({string.Join(", ", values)});");
        return new()
        {
            Node = rewritten.WithTriviaFrom(statement),
            Member = call.Member,
            ForAnyArguments = call.ForAnyArguments,
            Type = needsTasks ? call.Type : null,
        };
    }

    // SetupSequence itself first, then the step invocations in order.
    private static List<InvocationExpressionSyntax> Steps(InvocationExpressionSyntax sequence)
    {
        var chain = new List<InvocationExpressionSyntax> { sequence };
        ExpressionSyntax current = sequence;
        while (current.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax next } access
            && access.Expression == current && next.Expression == access && s_steps.Contains(access.Name.Identifier.Text))
        {
            chain.Add(next);
            current = next;
        }

        return chain;
    }
}
