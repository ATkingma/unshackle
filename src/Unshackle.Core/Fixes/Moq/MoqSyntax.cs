using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.Moq;

// Shared recognition of Moq's Mock<T>, Mock.Of and Mock.Get.
public static class MoqSyntax
{
    public static bool IsMockOfT(ITypeSymbol? type) =>
        type is INamedTypeSymbol { Name: "Mock", Arity: 1, ContainingNamespace: { Name: "Moq", ContainingNamespace.IsGlobalNamespace: true } };

    public static bool IsMockClass(ITypeSymbol? type) =>
        type is INamedTypeSymbol { Name: "Mock", Arity: 0, ContainingNamespace: { Name: "Moq", ContainingNamespace.IsGlobalNamespace: true } };

    public static ITypeSymbol MockedType(ITypeSymbol mockType) => ((INamedTypeSymbol)mockType).TypeArguments[0];

    public static bool IsObjectAccess(SyntaxNode? node, SyntaxNode receiver) =>
        node is MemberAccessExpressionSyntax { Name.Identifier.Text: "Object" } access && access.Expression == receiver;

    // Null when the creation is a plain loose mock; otherwise the Manual reason.
    public static string? CreationBlocker(BaseObjectCreationExpressionSyntax creation, SemanticModel model)
    {
        if (creation.Initializer is not null)
        {
            return "MockOptions: the mock sets options in an object initializer (CallBase, DefaultValue, ...); see moq-queue.md row 9";
        }

        SeparatedSyntaxList<ArgumentSyntax> arguments = creation.ArgumentList?.Arguments ?? default;
        if (arguments.Count == 0)
        {
            return null;
        }

        return arguments.Count == 1 ? BehaviorBlocker(arguments[0].Expression, model)
            : "ConstructorArguments: a mock with constructor arguments is a class mock; see moq-queue.md row 9";
    }

    // MockBehavior.Loose/Default is dropped (council round 8, M1); Strict and anything not literal stays Manual.
    public static string? BehaviorBlocker(ExpressionSyntax argument, SemanticModel model)
    {
        if (model.GetSymbolInfo(argument).Symbol is IFieldSymbol { ContainingType: { Name: "MockBehavior", ContainingNamespace.Name: "Moq" } } field)
        {
            return field.Name is "Loose" or "Default" ? null
                : "StrictBehaviour: NSubstitute has no strict mocks; a strict mock throws on calls without a setup (council round 8, M1)";
        }

        return "ConstructorArguments: the argument is not a literal MockBehavior.Loose/Default; see moq-queue.md row 9";
    }

    // The mocked type as the user wrote it, so aliases and short names stay.
    public static TypeSyntax TypeArgument(TypeSyntax? written, ITypeSymbol mocked, SemanticModel model, int position)
    {
        GenericNameSyntax? generic = written switch
        {
            GenericNameSyntax g => g,
            QualifiedNameSyntax { Right: GenericNameSyntax g } => g,
            AliasQualifiedNameSyntax { Name: GenericNameSyntax g } => g,
            _ => null,
        };
        return generic is { TypeArgumentList.Arguments.Count: 1 }
            ? generic.TypeArgumentList.Arguments[0].WithoutTrivia()
            : SyntaxFactory.ParseTypeName(mocked.ToMinimalDisplayString(model, position));
    }

    public static ExpressionSyntax SubstituteFor(TypeSyntax type) =>
        SyntaxFactory.ParseExpression($"Substitute.For<{type}>()");
}
