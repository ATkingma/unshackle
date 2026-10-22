using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Moq.Patterns;

// A mock that is only ever used as .Object becomes the substitute itself: the declaration gets the mocked type and
// Substitute.For<T>(), each .Object goes. Any other use (Setup, Verify, passing the Mock<T>) waits for its own queue row.
public class CreatePattern : IPattern
{
    private const string NSubstitute = "NSubstitute";

    public string Id => "moq.create";

    public string Description => "new Mock<T>() + .Object, Mock.Of<T>() → Substitute.For<T>()";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclarationSyntax declaration when MoqSyntax.IsMockOfT(model.GetTypeInfo(declaration.Type).Type):
                    yield return declaration;
                    break;
                case BaseObjectCreationExpressionSyntax creation when MoqSyntax.IsMockOfT(model.GetTypeInfo(creation).Type)
                        && !InMockDeclaration(creation, model):
                    yield return MoqSyntax.IsObjectAccess(creation.Parent, creation) ? creation.Parent! : creation;
                    break;
                case InvocationExpressionSyntax invocation when IsMockStatic(invocation, model):
                    yield return invocation;
                    break;
                case MemberAccessExpressionSyntax { Name.Identifier.Text: "Object" } access
                        when Holder(access.Expression, model) is { } holder && MoqSyntax.IsMockOfT(Type(holder))
                            && Blocker(holder, model) is null:
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

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => node switch
    {
        VariableDeclarationSyntax declaration => RewriteDeclaration(declaration, model),
        MemberAccessExpressionSyntax { Expression: BaseObjectCreationExpressionSyntax creation } => RewriteInline(creation, model),
        MemberAccessExpressionSyntax access => PatternResult.Fixed(access.Expression.WithoutTrivia()),
        BaseObjectCreationExpressionSyntax creation => RewriteAssigned(creation, model),
        InvocationExpressionSyntax invocation => RewriteStatic(invocation, model),
        ParameterSyntax => PatternResult.Manual("MockPassedAround: a Mock<T> parameter; the caller's mock and its setups move together, not yet supported"),
        _ => PatternResult.Manual("MockPassedAround: a Mock<T> property; its users can be anywhere, not yet supported"),
    };

    private static PatternResult RewriteDeclaration(VariableDeclarationSyntax declaration, SemanticModel model)
    {
        if (declaration.Variables.Count != 1 || model.GetDeclaredSymbol(declaration.Variables[0]) is not { } symbol)
        {
            return PatternResult.Manual("MockDeclaration: several mocks in one declaration; split it first");
        }

        if (Blocker(symbol, model) is { } blocker)
        {
            return PatternResult.Manual(blocker);
        }

        ITypeSymbol mocked = MoqSyntax.MockedType(Type(symbol)!);
        VariableDeclaratorSyntax variable = declaration.Variables[0];
        TypeSyntax written = declaration.Type.IsVar ? (variable.Initializer?.Value as ObjectCreationExpressionSyntax)?.Type ?? declaration.Type : declaration.Type;
        TypeSyntax mockedType = MoqSyntax.TypeArgument(written, mocked, model, declaration.SpanStart);
        TypeSyntax newType = declaration.Type.IsVar ? declaration.Type : mockedType.WithTriviaFrom(declaration.Type);
        if (variable.Initializer is not null)
        {
            variable = variable.WithInitializer(variable.Initializer.WithValue(
                MoqSyntax.SubstituteFor(mockedType).WithTriviaFrom(variable.Initializer.Value)));
        }

        return PatternResult.Fixed(declaration.WithType(newType).WithVariables(SyntaxFactory.SingletonSeparatedList(variable)), NSubstitute);
    }

    private static PatternResult RewriteInline(BaseObjectCreationExpressionSyntax creation, SemanticModel model)
    {
        if (MoqSyntax.CreationBlocker(creation, model) is { } blocker)
        {
            return PatternResult.Manual(blocker);
        }

        ITypeSymbol mocked = MoqSyntax.MockedType(model.GetTypeInfo(creation).Type!);
        if (MockedBlocker(mocked, mockOf: false) is { } typeBlocker)
        {
            return PatternResult.Manual(typeBlocker);
        }

        TypeSyntax? written = (creation as ObjectCreationExpressionSyntax)?.Type;
        return PatternResult.Fixed(MoqSyntax.SubstituteFor(MoqSyntax.TypeArgument(written, mocked, model, creation.SpanStart)), NSubstitute);
    }

    // `_mock = new Mock<T>()`: the target's declaration and .Object uses are rewritten with it, so it follows the target.
    private static PatternResult RewriteAssigned(BaseObjectCreationExpressionSyntax creation, SemanticModel model)
    {
        if (creation.Parent is not AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment
            || assignment.Right != creation || Holder(assignment.Left, model) is not { } target)
        {
            return PatternResult.Manual("MockPassedAround: the mock is passed or returned as Mock<T>, not yet supported");
        }

        if (Blocker(target, model) is { } blocker)
        {
            return PatternResult.Manual(blocker);
        }

        ITypeSymbol mocked = MoqSyntax.MockedType(Type(target)!);
        TypeSyntax? written = (creation as ObjectCreationExpressionSyntax)?.Type;
        return PatternResult.Fixed(MoqSyntax.SubstituteFor(MoqSyntax.TypeArgument(written, mocked, model, creation.SpanStart)), NSubstitute);
    }

    private static PatternResult RewriteStatic(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var method = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;
        if (method.Name == "Get")
        {
            return PatternResult.Manual("MockGet: Mock.Get(x) reaches the mock behind an object; its setups need moq-queue.md rows 3-4");
        }

        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count == 1 && method.Parameters[0].Type.Name == "MockBehavior")
        {
            if (MoqSyntax.BehaviorBlocker(arguments[0].Expression, model) is { } behavior)
            {
                return PatternResult.Manual(behavior);
            }
        }
        else if (arguments.Count > 0)
        {
            return PatternResult.Manual("MockOfSetup: Mock.Of<T>(predicate) sets members up; see moq-queue.md row 3");
        }

        ITypeSymbol mocked = method.TypeArguments[0];
        if (MockedBlocker(mocked, mockOf: true) is { } typeBlocker)
        {
            return PatternResult.Manual(typeBlocker);
        }

        TypeSyntax? written = invocation.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax name } ? name : null;
        return PatternResult.Fixed(MoqSyntax.SubstituteFor(MoqSyntax.TypeArgument(written, mocked, model, invocation.SpanStart)), NSubstitute);
    }

    private static bool InMockDeclaration(BaseObjectCreationExpressionSyntax creation, SemanticModel model) =>
        creation.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax declaration } }
        && MoqSyntax.IsMockOfT(model.GetTypeInfo(declaration.Type).Type);

    private static bool IsMockStatic(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { IsStatic: true, Name: "Of" or "Get" } method && MoqSyntax.IsMockClass(method.ContainingType);

    // The local or field an expression names (`m`, `_m`, `this._m`), or null.
    private static ISymbol? Holder(ExpressionSyntax expression, SemanticModel model) =>
        expression is IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }
            && model.GetSymbolInfo(expression).Symbol is ILocalSymbol or IFieldSymbol ? model.GetSymbolInfo(expression).Symbol : null;

    private static ITypeSymbol? Type(ISymbol symbol) => symbol switch
    {
        ILocalSymbol local => local.Type,
        IFieldSymbol field => field.Type,
        _ => null,
    };

    private static string? MockedBlocker(ITypeSymbol mocked, bool mockOf)
    {
        if (mocked.TypeKind != TypeKind.Interface)
        {
            return "ClassMock: only interface mocks are converted; a class or delegate mock is moq-queue.md row 9";
        }

        return MockDefaults.Difference(mocked, mockOf) is { } difference
            ? $"DefaultValueDiffers: an unconfigured {mocked.Name} answers differently in NSubstitute: {difference}"
            : null;
    }

    // Null when the local or field is only created as a plain loose mock and only used as .Object; otherwise the Manual reason.
    private static string? Blocker(ISymbol symbol, SemanticModel model)
    {
        if (symbol.DeclaringSyntaxReferences is not [var reference] || reference.SyntaxTree != model.SyntaxTree)
        {
            return "MockPassedAround: the mock is declared in another file";
        }

        SyntaxNode declarator = reference.GetSyntax();
        if (declarator.Parent is VariableDeclarationSyntax { Variables.Count: > 1 })
        {
            return "MockDeclaration: several mocks in one declaration; split it first";
        }

        SyntaxNode? scope;
        if (symbol is IFieldSymbol field)
        {
            if (field.DeclaredAccessibility != Accessibility.Private || field.ContainingType.DeclaringSyntaxReferences.Length != 1)
            {
                return "MockPassedAround: a non-private field (or one of a partial class) can be used outside this file";
            }

            scope = field.ContainingType.DeclaringSyntaxReferences[0].GetSyntax();
        }
        else
        {
            scope = declarator.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? declarator.SyntaxTree.GetRoot();
        }

        ITypeSymbol mocked = MoqSyntax.MockedType(Type(symbol)!);
        var creations = new List<BaseObjectCreationExpressionSyntax>();
        if (declarator is VariableDeclaratorSyntax { Initializer.Value: var initial })
        {
            if (initial is not BaseObjectCreationExpressionSyntax creation)
            {
                return "MockPassedAround: the mock comes from another expression than new Mock<T>()";
            }

            creations.Add(creation);
        }

        foreach (IdentifierNameSyntax identifier in scope.DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(i => i.Identifier.Text == symbol.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(i).Symbol, symbol)))
        {
            ExpressionSyntax use = identifier.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } self && self.Name == identifier ? self : identifier;
            if (MoqSyntax.IsObjectAccess(use.Parent, use))
            {
                continue;
            }

            if (use.Parent is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Right: BaseObjectCreationExpressionSyntax assigned } assignment
                && assignment.Left == use)
            {
                creations.Add(assigned);
                continue;
            }

            string shown = (use.Parent as MemberAccessExpressionSyntax)?.ToString() ?? use.Parent?.ToString() ?? symbol.Name;
            return $"MockUsedBeyondObject: {symbol.Name} is used as a Mock<T> (`{Shorten(shown)}`); that use needs a later moq-queue.md row";
        }

        foreach (BaseObjectCreationExpressionSyntax creation in creations)
        {
            if (MoqSyntax.CreationBlocker(creation, model) is { } blocker)
            {
                return blocker;
            }
        }

        return MockedBlocker(mocked, mockOf: false);
    }

    private static string Shorten(string text)
    {
        string line = text.ReplaceLineEndings(" ");
        return line.Length <= 60 ? line : line[..57] + "...";
    }
}
