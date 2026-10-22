using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// A mock whose every remaining use is .Object becomes the substitute itself: the declaration gets the mocked type and
// Substitute.For<T>(), each .Object goes. Moq's null defaults that NSubstitute answers differently are pinned right after the
// creation (council round 9, Q1). The patterns for Setup/Verify run first and turn their uses into .Object uses.
public class CreatePattern : IPattern
{
    private const string NSubstitute = "NSubstitute";

    private const string ReturnsExtensions = "NSubstitute.ReturnsExtensions";

    public virtual string Id => "moq.create";

    public virtual string Description => "new Mock<T>() + .Object, Mock.Of<T>() → Substitute.For<T>()";

    // moq.create takes every mock except class and delegate mocks, which moq.class-mocks (row 9) reports under its own id; null: no type.
    protected virtual bool Owns(ITypeSymbol? mocked) => mocked is null || mocked.TypeKind is not (TypeKind.Class or TypeKind.Delegate);

    private bool OwnsMock(ITypeSymbol? mockType) => Owns(MoqSyntax.IsMockOfT(mockType) ? MoqSyntax.MockedType(mockType!) : null);

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclarationSyntax declaration when MoqSyntax.IsMockOfT(model.GetTypeInfo(declaration.Type).Type) && OwnsMock(model.GetTypeInfo(declaration.Type).Type):
                    yield return declaration.Parent is LocalDeclarationStatementSyntax statement ? statement : declaration;
                    break;
                case BaseObjectCreationExpressionSyntax creation when MoqSyntax.IsMockOfT(model.GetTypeInfo(creation).Type) && OwnsMock(model.GetTypeInfo(creation).Type) && !InMockDeclaration(creation, model):
                    yield return MoqSyntax.IsObjectAccess(creation.Parent, creation) ? creation.Parent!
                        : creation.Parent is AssignmentExpressionSyntax { Parent: ExpressionStatementSyntax assignment } ? assignment
                        : creation;
                    break;
                case InvocationExpressionSyntax tracking when Owns(null) && PropertySyntax.IsTracking(tracking, model):
                    yield return tracking.Parent!;
                    break;
                case InvocationExpressionSyntax invocation when IsMockStatic(invocation, model) && Owns(StaticMocked(invocation, model)):
                    yield return invocation;
                    break;
                case MemberAccessExpressionSyntax { Name.Identifier.Text: "Object" } access
                        when MockUsage.Holder(access.Expression, model) is { } holder && MoqSyntax.IsMockOfT(MockUsage.Type(holder)) && OwnsMock(MockUsage.Type(holder))
                            && MockUsage.Analyze(holder, model, pendingUses: false).Blocker is null:
                    yield return access;
                    break;
                case ParameterSyntax { Type: not null } parameter when Owns(null) && MoqSyntax.IsMockOfT(model.GetTypeInfo(parameter.Type).Type):
                    yield return parameter;
                    break;
                case PropertyDeclarationSyntax property when Owns(null) && MoqSyntax.IsMockOfT(model.GetTypeInfo(property.Type).Type):
                    yield return property;
                    break;
                default:
                    break;
            }
        }
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        MoqTargetFramework.Unsupported(node) is { } framework
            ? PatternResult.Manual(MoqReason.TargetFrameworkUnsupported, MoqTargetFramework.Reason(framework))
            : RewriteSupported(node, model);

    private static PatternResult RewriteSupported(SyntaxNode node, SemanticModel model) => node switch
    {
        LocalDeclarationStatementSyntax statement => RewriteDeclaration(statement.Declaration, model, statement),
        ExpressionStatementSyntax { Expression: InvocationExpressionSyntax tracking } when PropertySyntax.IsTracking(tracking, model) => RewriteTracking(tracking, model),
        VariableDeclarationSyntax declaration => RewriteDeclaration(declaration, model, null),
        ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Right: BaseObjectCreationExpressionSyntax creation } } statement =>
            RewriteAssigned(creation, model, statement),
        MemberAccessExpressionSyntax { Expression: BaseObjectCreationExpressionSyntax creation } => RewriteInline(creation, model),
        MemberAccessExpressionSyntax access => PatternResult.Fixed(access.Expression.WithoutTrivia()),
        BaseObjectCreationExpressionSyntax creation => RewriteAssigned(creation, model, null),
        InvocationExpressionSyntax invocation => RewriteStatic(invocation, model),
        ParameterSyntax => PatternResult.Manual(MoqReason.MockPassedAround, "a Mock<T> parameter; the caller's mock and its setups move together, not yet supported"),
        _ => PatternResult.Manual(MoqReason.MockPassedAround, "a Mock<T> property; its users can be anywhere, not yet supported"),
    };

    private static PatternResult RewriteDeclaration(VariableDeclarationSyntax declaration, SemanticModel model, LocalDeclarationStatementSyntax? statement)
    {
        if (declaration.Variables.Count != 1 || model.GetDeclaredSymbol(declaration.Variables[0]) is not { } symbol)
        {
            return PatternResult.Manual(MoqReason.MockDeclaration, "several mocks in one declaration; split it first");
        }

        MockAnalysis analysis = MockUsage.Analyze(symbol, model, pendingUses: false);
        if (analysis.Blocker is not null)
        {
            return analysis.Blocker.ToResult();
        }

        ITypeSymbol mocked = analysis.Mocked!;
        VariableDeclaratorSyntax variable = declaration.Variables[0];
        TypeSyntax written = declaration.Type.IsVar ? (variable.Initializer?.Value as ObjectCreationExpressionSyntax)?.Type ?? declaration.Type : declaration.Type;
        TypeSyntax mockedType = MoqSyntax.TypeArgument(written, mocked, model, declaration.SpanStart);
        TypeSyntax newType = declaration.Type.IsVar ? declaration.Type : mockedType.WithTriviaFrom(declaration.Type);
        if (variable.Initializer is not null)
        {
            variable = variable.WithInitializer(variable.Initializer.WithValue(
                (variable.Initializer.Value is BaseObjectCreationExpressionSyntax initial ? MoqSyntax.SubstituteFor(mockedType, initial, mocked, model) : MoqSyntax.SubstituteFor(mockedType)).WithTriviaFrom(variable.Initializer.Value)));
        }

        VariableDeclarationSyntax newDeclaration = declaration.WithType(newType).WithVariables(SyntaxFactory.SingletonSeparatedList(variable));
        if (statement is null || variable.Initializer is null || analysis.Pins.Count == 0)
        {
            return WithInfo(statement is null ? newDeclaration : statement.WithDeclaration(newDeclaration), [], analysis);
        }

        return WithInfo(statement.WithDeclaration(newDeclaration), Pins(SyntaxFactory.IdentifierName(symbol.Name), analysis.Pins, model, statement.SpanStart), analysis);
    }

    // `_mock = new Mock<T>()`: the target's declaration and .Object uses are rewritten with it, so it follows the target.
    private static PatternResult RewriteAssigned(BaseObjectCreationExpressionSyntax creation, SemanticModel model, ExpressionStatementSyntax? statement)
    {
        if (creation.Parent is not AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment
            || assignment.Right != creation || MockUsage.Holder(assignment.Left, model) is not { } target)
        {
            return PatternResult.Manual(MoqReason.MockPassedAround, "the mock is passed or returned as Mock<T>, not yet supported");
        }

        MockAnalysis analysis = MockUsage.Analyze(target, model, pendingUses: false);
        if (analysis.Blocker is not null)
        {
            return analysis.Blocker.ToResult();
        }

        TypeSyntax? written = (creation as ObjectCreationExpressionSyntax)?.Type;
        ExpressionSyntax substitute = MoqSyntax.SubstituteFor(MoqSyntax.TypeArgument(written, analysis.Mocked!, model, creation.SpanStart), creation, analysis.Mocked!, model).WithTriviaFrom(creation);
        if (statement is null)
        {
            return WithInfo(substitute, [], analysis);
        }

        SyntaxNode newStatement = statement.ReplaceNode(creation, substitute);
        return WithInfo(newStatement, Pins(assignment.Left.WithoutTrivia(), analysis.Pins, model, statement.SpanStart), analysis);
    }

    private static PatternResult RewriteInline(BaseObjectCreationExpressionSyntax creation, SemanticModel model)
    {
        if (MoqSyntax.CreationBlocker(creation, model) is { } blocker)
        {
            return blocker.ToResult();
        }

        ITypeSymbol mocked = MoqSyntax.MockedType(model.GetTypeInfo(creation).Type!);
        if (InlineBlocker(mocked, mockOf: false) is { } typeBlocker)
        {
            return typeBlocker.ToResult();
        }

        TypeSyntax? written = (creation as ObjectCreationExpressionSyntax)?.Type;
        return PatternResult.Fixed(MoqSyntax.SubstituteFor(MoqSyntax.TypeArgument(written, mocked, model, creation.SpanStart), creation, mocked, model), NSubstitute);
    }

    private static PatternResult RewriteStatic(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var method = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;
        if (method.Name == "Get")
        {
            return PatternResult.Manual(MoqReason.MockGet, "Mock.Get(x) reaches the mock behind an object; its setups need moq-queue.md rows 3-4");
        }

        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 1 && method.Parameters[0].Type.Name == "MockBehavior")
        {
            if (MoqSyntax.BehaviorBlocker(arguments[0].Expression, model) is { } behavior)
            {
                return behavior.ToResult();
            }
        }
        else if (arguments.Count > 0)
        {
            return PatternResult.Manual(MoqReason.MockOfSetup, "Mock.Of<T>(predicate) sets members up; see moq-queue.md row 3");
        }

        ITypeSymbol mocked = method.TypeArguments[0];
        if (InlineBlocker(mocked, mockOf: true) is { } typeBlocker)
        {
            return typeBlocker.ToResult();
        }

        TypeSyntax? written = invocation.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax name } ? name : null;
        return PatternResult.Fixed(MoqSyntax.SubstituteFor(MoqSyntax.TypeArgument(written, mocked, model, invocation.SpanStart)), NSubstitute);
    }

    // Row 5: SetupProperty/SetupAllProperties go (an initial value becomes Returns), in the same pass as the declaration they let through.
    private static PatternResult RewriteTracking(InvocationExpressionSyntax tracking, SemanticModel model)
    {
        if (MockUsage.Holder(((MemberAccessExpressionSyntax)tracking.Expression).Expression, model) is not { } holder)
        {
            return PatternResult.Manual(MoqReason.MockPassedAround, "the property setup is on a mock that is not a local or private field of this file");
        }

        MockAnalysis analysis = MockUsage.Analyze(holder, model, pendingUses: false);
        return analysis.Blocker is not null ? analysis.Blocker.ToResult() : PatternResult.Fixed(PropertySyntax.Rewrite(tracking, model), NSubstitute);
    }

    // An inline mock has no statement to pin defaults after, so any difference blocks it.
    private static MoqBlocker? InlineBlocker(ITypeSymbol mocked, bool mockOf)
    {
        // new Mock<C>(args) { CallBase = true }.Object was accepted by CreationBlocker already.
        if (mocked.TypeKind == TypeKind.Class && !mockOf)
        {
            return null;
        }

        if (mocked.TypeKind != TypeKind.Interface)
        {
            return MoqBlocker.Of(MoqReason.ClassMock, "Mock.Of<T>() of a class or delegate answers every member with a mock (DefaultValue.Mock); no proven NSubstitute form (moq.class-mocks)");
        }

        DefaultsPlan plan = MockDefaults.Plan(mocked, mockOf, [], [], allTracked: false);
        return plan.Blocker ?? (plan.Pins.Count == 0 ? null
            : MoqBlocker.Of(MoqReason.DefaultValueDiffers, $"an unconfigured {mocked.Name} answers differently in NSubstitute ({string.Join(", ", plan.Pins.Select(p => p.Name))}); "
                + "pinning Moq's default needs the mock in a local or field"));
    }

    // `m.M(Arg.Any<A>(), ...).ReturnsNull();` / `m.P.ReturnsNull();` per pinned member.
    private static IReadOnlyList<StatementSyntax> Pins(ExpressionSyntax receiver, IReadOnlyList<ISymbol> pins, SemanticModel model, int position) =>
        [.. pins.Select(pin => SyntaxFactory.ParseStatement(pin switch
        {
            IMethodSymbol method => $"{receiver}.{method.Name}({string.Join(", ", method.Parameters.Select(p => $"Arg.Any<{p.Type.ToMinimalDisplayString(model, position)}>()"))}).ReturnsNull();",
            _ => $"{receiver}.{pin.Name}.ReturnsNull();",
        }))];

    private static PatternResult WithInfo(SyntaxNode node, IReadOnlyList<StatementSyntax> pins, MockAnalysis analysis)
    {
        var info = new List<string>();
        if (analysis.Unused)
        {
            info.Add("mock never used");
        }

        if (pins.Count > 0)
        {
            info.Add("pinned Moq's default null for " + string.Join(", ", analysis.Pins.Select(p => p.Name)));
        }

        PatternResult result = pins.Count == 0 ? PatternResult.Fixed(node, NSubstitute)
            : PatternResult.Fixed([node, .. pins], NSubstitute, ReturnsExtensions);
        return info.Count == 0 ? result : (FixedResult)result with { Info = string.Join("; ", info) };
    }

    private static bool InMockDeclaration(BaseObjectCreationExpressionSyntax creation, SemanticModel model) =>
        creation.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } }
        && MoqSyntax.IsMockOfT(model.GetTypeInfo(declaration.Type).Type);

    private static ITypeSymbol? StaticMocked(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Name: "Of", TypeArguments: [var mocked] } ? mocked : null;

    private static bool IsMockStatic(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { IsStatic: true, Name: "Of" or "Get" } method && MoqSyntax.IsMockClass(method.ContainingType);
}
