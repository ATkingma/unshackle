using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Polly;

// Policy.WrapAsync(a, b, c) and a.WrapAsync(b) → one builder with AddRetry per operand, outermost first (council round 8, P4; queue
// row 10). Fixed only when every operand is a retry call written in place or a local created by one and used only here.
internal static class PollyWrap
{
    public static bool IsWrap(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WrapAsync" }
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ContainingAssembly.Name: PollyRetry.PollyAssembly };

    // The operands outermost first: Policy.WrapAsync(a, b) and a.WrapAsync(b) both mean a around b; nested wraps flatten.
    public static List<ExpressionSyntax>? Operands(InvocationExpressionSyntax wrap, SemanticModel model)
    {
        if (model.GetSymbolInfo(wrap).Symbol is not IMethodSymbol method || wrap.ArgumentList.Arguments.Any(a => a.NameColon is not null))
        {
            return null;
        }

        List<ExpressionSyntax> operands = [];
        // a.WrapAsync(b): an instance or extension call, so the receiver is the outer operand.
        if ((!method.IsStatic || method.ReducedFrom is not null) && wrap.Expression is MemberAccessExpressionSyntax access)
        {
            operands.Add(access.Expression);
        }

        operands.AddRange(wrap.ArgumentList.Arguments.Select(a => a.Expression));
        List<ExpressionSyntax> flat = [];
        foreach (ExpressionSyntax operand in operands)
        {
            if (operand is InvocationExpressionSyntax inner && IsWrap(inner, model))
            {
                if (Operands(inner, model) is not { } nested)
                {
                    return null;
                }

                flat.AddRange(nested);
            }
            else
            {
                flat.Add(operand);
            }
        }

        return flat.Count >= 2 ? flat : null;
    }

    public static PollyPlan Plan(InvocationExpressionSyntax wrap, SemanticModel model)
    {
        if (Operands(wrap, model) is not { } operands)
        {
            return NotProvable("the wrap has fewer than two operands, named arguments or an array argument.");
        }

        string indent = PollyRetry.Indent(wrap);
        string newLine = PollyRetry.NewLine(wrap);
        List<string> strategies = [];
        HashSet<string?> resultTypes = [];
        foreach (ExpressionSyntax operand in operands)
        {
            if (OperandCreation(operand, wrap, model) is not InvocationExpressionSyntax creation)
            {
                return NotProvable($"the operand '{operand}' is not a retry call written in place or a local created by one and used only in this wrap.");
            }

            resultTypes.Add(PollyRetry.ResultType(creation, model));
            PollyPlan options = PollyRetry.PlanOptions(creation, model, indent, wrap.Contains(creation) ? null : wrap);
            if (options.Text is not string text)
            {
                return options;
            }

            strategies.Add($"{newLine}{indent}    .AddRetry({text})");
        }

        if (resultTypes.Count != 1)
        {
            return NotProvable("the operands mix result types.");
        }

        string generic = resultTypes.Single() is string result ? $"<{result}>" : string.Empty;
        return PollyPlan.Of(SyntaxFactory.ParseExpression($"new ResiliencePipelineBuilder{generic}(){string.Concat(strategies)}{newLine}{indent}    .Build()"));
    }

    // The retry call behind an operand: written in place, or the creation of a local whose only use is this operand.
    public static InvocationExpressionSyntax? OperandCreation(ExpressionSyntax operand, InvocationExpressionSyntax wrap, SemanticModel model)
    {
        if (operand is InvocationExpressionSyntax inline)
        {
            return PollyRetry.IsRetryCreation(inline, model) ? inline : null;
        }

        return operand is IdentifierNameSyntax name && OperandLocal(name, model) is (_, InvocationExpressionSyntax creation) && wrap.Contains(operand)
            ? creation
            : null;
    }

    // The declarations of locals that only feed a wrap: the wrap rewrite inlines them, so they go.
    public static IEnumerable<LocalDeclarationStatementSyntax> OperandLocals(InvocationExpressionSyntax wrap, SemanticModel model) =>
        (Operands(wrap, model) ?? []).OfType<IdentifierNameSyntax>()
            .Select(n => OperandLocal(n, model))
            .Where(l => l is not null)
            .Select(l => l!.Value.Declaration);

    // A retry call that a wrap consumes (in place, or as a local used only there): the wrap pattern owns it.
    public static bool IsOperand(InvocationExpressionSyntax creation, SemanticModel model)
    {
        SyntaxNode? holder = creation.Parent switch
        {
            ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax wrap } when IsWrap(wrap, model) => wrap,
            MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WrapAsync", Parent: InvocationExpressionSyntax wrap } when IsWrap(wrap, model) => wrap,
            _ => null,
        };
        if (holder is not null)
        {
            return true;
        }

        return creation.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator }
            && model.GetDeclaredSymbol(declarator) is ILocalSymbol local
            && SingleUse(local, declarator, model) is IdentifierNameSyntax use
            && OperandLocal(use, model) is not null;
    }

    public static PollyPlan RemoveOperandLocal(LocalDeclarationStatementSyntax statement, SemanticModel model)
    {
        if (statement.Declaration.Variables is not [VariableDeclaratorSyntax declarator]
            || model.GetDeclaredSymbol(declarator) is not ILocalSymbol local
            || SingleUse(local, declarator, model) is not IdentifierNameSyntax use
            || use.Ancestors().OfType<InvocationExpressionSyntax>().FirstOrDefault(i => IsWrap(i, model)) is not InvocationExpressionSyntax wrap)
        {
            return NotProvable("the local is not used only as a wrap operand.");
        }

        // The outermost wrap of a nested chain decides.
        InvocationExpressionSyntax outer = wrap;
        while (Enclosing(outer) is InvocationExpressionSyntax next && IsWrap(next, model))
        {
            outer = next;
        }

        // Text, not a node: the caller removes the declaration (Fixed([])) when the wrap's own rewrite goes ahead.
        PollyPlan plan = PollyRetry.PlanWrapUse(outer, model);
        return plan.Node is null ? plan : PollyPlan.OfText(string.Empty);
    }

    private static InvocationExpressionSyntax? Enclosing(InvocationExpressionSyntax wrap) => wrap.Parent switch
    {
        MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax next } => next,
        ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax next } => next,
        _ => null,
    };

    private static (LocalDeclarationStatementSyntax Declaration, InvocationExpressionSyntax Creation)? OperandLocal(IdentifierNameSyntax use, SemanticModel model)
    {
        if (model.GetSymbolInfo(use).Symbol is not ILocalSymbol local
            || local.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is not VariableDeclaratorSyntax
            {
                Initializer.Value: InvocationExpressionSyntax creation, Parent.Parent: LocalDeclarationStatementSyntax declaration,
            } declarator
            || declaration.Declaration.Variables.Count != 1
            || !PollyRetry.IsRetryCreation(creation, model)
            || SingleUse(local, declarator, model) != use
            || use.Parent is not (ArgumentSyntax or MemberAccessExpressionSyntax { Name.Identifier.ValueText: "WrapAsync" }))
        {
            return null;
        }

        InvocationExpressionSyntax? wrap = use.Parent switch
        {
            ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax w } => w,
            MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax w } => w,
            _ => null,
        };
        return wrap is not null && IsWrap(wrap, model) ? (declaration, creation) : null;
    }

    private static IdentifierNameSyntax? SingleUse(ILocalSymbol local, VariableDeclaratorSyntax declarator, SemanticModel model)
    {
        List<IdentifierNameSyntax> uses =
        [
            .. declarator.SyntaxTree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>()
                .Where(n => n.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, local)),
        ];
        return uses is [IdentifierNameSyntax only] ? only : null;
    }

    private static PollyPlan NotProvable(string text) => PollyPlan.Manual(PollyReason.WrapOrderNotProvable, text);
}
