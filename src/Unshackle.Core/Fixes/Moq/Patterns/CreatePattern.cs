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

    public string Id => "moq.create";

    public string Description => "new Mock<T>() + .Object, Mock.Of<T>() → Substitute.For<T>()";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclarationSyntax declaration when MoqSyntax.IsMockOfT(model.GetTypeInfo(declaration.Type).Type):
                    yield return declaration.Parent is LocalDeclarationStatementSyntax statement ? statement : declaration;
                    break;
                case BaseObjectCreationExpressionSyntax creation when MoqSyntax.IsMockOfT(model.GetTypeInfo(creation).Type) && !InMockDeclaration(creation, model):
                    yield return MoqSyntax.IsObjectAccess(creation.Parent, creation) ? creation.Parent!
                        : creation.Parent is AssignmentExpressionSyntax { Parent: ExpressionStatementSyntax assignment } ? assignment
                        : creation;
                    break;
                case InvocationExpressionSyntax invocation when IsMockStatic(invocation, model):
                    yield return invocation;
                    break;
                case MemberAccessExpressionSyntax { Name.Identifier.Text: "Object" } access
                        when MockUsage.Holder(access.Expression, model) is { } holder && MoqSyntax.IsMockOfT(MockUsage.Type(holder))
                            && MockUsage.Analyze(holder, model, pendingUses: false).Blocker is null:
                    yield return access;
                    break;
                case ParameterSyntax { Type: not null } parameter when MoqSyntax.IsMockOfT(model.GetTypeInfo(parameter.Type).Type):
                    yield return parameter;
                    break;
                case PropertyDeclarationSyntax property when MoqSyntax.IsMockOfT(model.GetTypeInfo(property.Type).Type):
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
                MoqSyntax.SubstituteFor(mockedType).WithTriviaFrom(variable.Initializer.Value)));
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
        ExpressionSyntax substitute = MoqSyntax.SubstituteFor(MoqSyntax.TypeArgument(written, analysis.Mocked!, model, creation.SpanStart)).WithTriviaFrom(creation);
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
        return PatternResult.Fixed(MoqSyntax.SubstituteFor(MoqSyntax.TypeArgument(written, mocked, model, creation.SpanStart)), NSubstitute);
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

    // An inline mock has no statement to pin defaults after, so any difference blocks it.
    private static MoqBlocker? InlineBlocker(ITypeSymbol mocked, bool mockOf)
    {
        if (mocked.TypeKind != TypeKind.Interface)
        {
            return MoqBlocker.Of(MoqReason.ClassMock, "only interface mocks are converted; a class or delegate mock is moq-queue.md row 9");
        }

        DefaultsPlan plan = MockDefaults.Plan(mocked, mockOf, []);
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

    private static bool IsMockStatic(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { IsStatic: true, Name: "Of" or "Get" } method && MoqSyntax.IsMockClass(method.ContainingType);
}
