using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// One local or private field of type Mock<T>: may it become a substitute, and which defaults are pinned? Every pattern of the fix
// asks this for the same mock and gets the same answer, so a mock is rewritten everywhere or nowhere.
public static class MockUsage
{
    // pendingUses: the uses other patterns rewrite (Setup, Verify) count as rewritten. moq.create passes false: it runs last, so any
    // such use still there was not rewritten and the mock must stay Moq.
    public static MockAnalysis Analyze(ISymbol symbol, SemanticModel model, bool pendingUses)
    {
        if (symbol.DeclaringSyntaxReferences is [var declared, ..] && MoqTargetFramework.Unsupported(declared.GetSyntax()) is { } framework)
        {
            return MockAnalysis.Blocked(MoqReason.TargetFrameworkUnsupported, MoqTargetFramework.Reason(framework));
        }

        if (Type(symbol) is not { } mockType || !MoqSyntax.IsMockOfT(mockType))
        {
            return MockAnalysis.Blocked(MoqReason.MockPassedAround, "not a Mock<T> local or field");
        }

        if (symbol.DeclaringSyntaxReferences is not [var reference] || reference.SyntaxTree != model.SyntaxTree)
        {
            return MockAnalysis.Blocked(MoqReason.MockPassedAround, "the mock is declared in another file");
        }

        SyntaxNode declarator = reference.GetSyntax();
        if (declarator.Parent is VariableDeclarationSyntax { Variables.Count: > 1 })
        {
            return MockAnalysis.Blocked(MoqReason.MockDeclaration, "several mocks in one declaration; split it first");
        }

        SyntaxNode scope;
        if (symbol is IFieldSymbol field)
        {
            if (field.DeclaredAccessibility != Accessibility.Private || field.ContainingType.DeclaringSyntaxReferences.Length != 1)
            {
                return MockAnalysis.Blocked(MoqReason.MockPassedAround, "a non-private field (or one of a partial class) can be used outside this file");
            }

            scope = field.ContainingType.DeclaringSyntaxReferences[0].GetSyntax();
        }
        else
        {
            scope = declarator.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? declarator.SyntaxTree.GetRoot();
        }

        ITypeSymbol mocked = MoqSyntax.MockedType(mockType);
        if (mocked.TypeKind != TypeKind.Interface)
        {
            return MockAnalysis.Blocked(MoqReason.ClassMock, "only interface mocks are converted; a class or delegate mock is moq-queue.md row 9");
        }

        var creations = new List<BaseObjectCreationExpressionSyntax>();
        if (declarator is VariableDeclaratorSyntax { Initializer.Value: var initial })
        {
            if (initial is not BaseObjectCreationExpressionSyntax creation)
            {
                return MockAnalysis.Blocked(MoqReason.MockPassedAround, "the mock comes from another expression than new Mock<T>()");
            }

            creations.Add(creation);
        }

        var context = new MockUseContext();
        int uses = 0;
        foreach (IdentifierNameSyntax identifier in scope.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.Text == symbol.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(i).Symbol, symbol)))
        {
            ExpressionSyntax use = Use(identifier);
            if (use.Parent is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Right: BaseObjectCreationExpressionSyntax assigned } assignment
                && assignment.Left == use)
            {
                creations.Add(assigned);
                continue;
            }

            uses++;
            if (MoqSyntax.IsObjectAccess(use.Parent, use))
            {
                if (SetUpForAnyArguments(use.Parent!, model) is { } member)
                {
                    context.SetUp.Add(member);
                }

                continue;
            }

            if (MockUseRules.Blocker(use, model, context, pendingUses) is { } blocker)
            {
                return MockAnalysis.Blocked(blocker);
            }
        }

        foreach (BaseObjectCreationExpressionSyntax creation in creations)
        {
            if (MoqSyntax.CreationBlocker(creation, model) is { } blocker)
            {
                return MockAnalysis.Blocked(blocker);
            }
        }

        if (context.TrackingPositions.Any(p => p > context.FirstConfiguration))
        {
            return MockAnalysis.Blocked(MoqReason.PropertyOrder, "SetupProperty/SetupAllProperties after another setup or verify of the mock; Moq may then reset that setup");
        }

        DefaultsPlan plan = MockDefaults.Plan(mocked, mockOf: false, context.SetUp, context.Tracked, context.AllTracked);
        if (plan.Blocker is not null)
        {
            return MockAnalysis.Blocked(plan.Blocker);
        }

        if (plan.Pins.Count > 0 && !creations.All(IsStatementCreation))
        {
            return MockAnalysis.Blocked(MoqReason.DefaultValueDiffers, $"an unconfigured {mocked.Name} answers differently in NSubstitute ("
                + string.Join(", ", plan.Pins.Select(p => p.Name)) + "); pinning Moq's default needs the creation as a statement, not a field initializer");
        }

        return new() { Pins = plan.Pins, Unused = uses == 0, Mocked = mocked };
    }

    // `var m = new Mock<T>();` or `m = new Mock<T>();`: the pins can follow it as statements.
    private static bool IsStatementCreation(BaseObjectCreationExpressionSyntax creation) => creation.Parent switch
    {
        EqualsValueClauseSyntax { Parent.Parent: VariableDeclarationSyntax { Parent: LocalDeclarationStatementSyntax } } => true,
        AssignmentExpressionSyntax { Parent: ExpressionStatementSyntax } => true,
        _ => false,
    };

    // `m`, or `this.m` when the identifier is the name of such an access.
    public static ExpressionSyntax Use(IdentifierNameSyntax identifier) =>
        identifier.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } self && self.Name == identifier ? self : identifier;

    // The local or field an expression names (`m`, `_m`, `this._m`), or null.
    public static ISymbol? Holder(ExpressionSyntax expression, SemanticModel model) =>
        expression is IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }
            && model.GetSymbolInfo(expression).Symbol is ILocalSymbol or IFieldSymbol ? model.GetSymbolInfo(expression).Symbol : null;

    public static ITypeSymbol? Type(ISymbol symbol) => symbol switch
    {
        ILocalSymbol local => local.Type,
        IFieldSymbol field => field.Type,
        _ => null,
    };

    // `m.Object.M(Arg.Any<A>(), ...).Returns(..)` (or a property): the member is set up for every argument, so it needs no pin.
    private static ISymbol? SetUpForAnyArguments(SyntaxNode objectAccess, SemanticModel model)
    {
        if (objectAccess.Parent is not MemberAccessExpressionSyntax memberAccess || memberAccess.Expression != objectAccess)
        {
            return null;
        }

        SyntaxNode call = memberAccess.Parent is InvocationExpressionSyntax invocation && invocation.Expression == memberAccess ? invocation : memberAccess;
        if (call.Parent is not MemberAccessExpressionSyntax { Name.Identifier.Text: var configure, Parent: InvocationExpressionSyntax } configuration
            || configuration.Expression != call || !configure.StartsWith("Returns", StringComparison.Ordinal))
        {
            return null;
        }

        bool anyArguments = call is not InvocationExpressionSyntax withArguments || withArguments.ArgumentList.Arguments.All(a =>
            model.GetSymbolInfo(a.Expression).Symbol is IMethodSymbol { Name: "Any", ContainingType: { Name: "Arg", ContainingNamespace.Name: "NSubstitute" } });
        return anyArguments ? model.GetSymbolInfo(call).Symbol : null;
    }
}
