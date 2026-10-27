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

    // Null when the creation is a plain loose mock; otherwise the Manual reason. A class mock (moq-queue.md row 9) has its own rules.
    public static MoqBlocker? CreationBlocker(BaseObjectCreationExpressionSyntax creation, SemanticModel model)
    {
        if (model.GetTypeInfo(creation).Type is { } mockType && IsMockOfT(mockType) && MockedType(mockType).TypeKind == TypeKind.Class)
        {
            return ClassCreation(creation, model, out _);
        }

        if (creation.Initializer is not null)
        {
            return MoqBlocker.Of(MoqReason.MockOptions, "the mock sets options in an object initializer (CallBase, DefaultValue, ...); see moq-queue.md row 9");
        }

        SeparatedSyntaxList<ArgumentSyntax> arguments = creation.ArgumentList?.Arguments ?? default;
        if (arguments.Count == 0)
        {
            return null;
        }

        return arguments.Count == 1 ? BehaviorBlocker(arguments[0].Expression, model)
            : MoqBlocker.Of(MoqReason.ConstructorArguments, "a mock with constructor arguments is a class mock; see moq-queue.md row 9");
    }

    // `new Mock<C>(args) { CallBase = true }` (also `new Mock<C>(MockBehavior.Loose, args)`) becomes `Substitute.ForPartsOf<C>(args)`:
    // measured run-time equal for the constructor, virtual and non-virtual members, properties, protected members, ToString/Equals and
    // virtual calls from the constructor. Without CallBase = true Moq answers every virtual member with its default, which a
    // NSubstitute substitute answers differently; an abstract class answers its abstract members differently (string "" vs null).
    public static MoqBlocker? ClassCreation(BaseObjectCreationExpressionSyntax creation, SemanticModel model, out IReadOnlyList<ExpressionSyntax> arguments)
    {
        arguments = [];
        ITypeSymbol mocked = MockedType(model.GetTypeInfo(creation).Type!);
        if (mocked.IsAbstract)
        {
            return MoqBlocker.Of(MoqReason.ClassMockAbstract, $"{mocked.Name} is abstract: an unconfigured abstract member answers Moq's default (null for a string) and NSubstitute's (\"\")");
        }

        if (creation.Initializer is not { Expressions: [AssignmentExpressionSyntax { Left: var left, Right: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.TrueLiteralExpression } }] }
            || model.GetSymbolInfo(left).Symbol is not IPropertySymbol { Name: "CallBase", ContainingType.Name: "Mock" })
        {
            return creation.Initializer is null
                ? MoqBlocker.Of(MoqReason.ClassMockNoCallBase, $"a {mocked.Name} mock without CallBase = true answers virtual members with Moq's default; a NSubstitute substitute answers differently (null vs \"\", measured)")
                : MoqBlocker.Of(MoqReason.MockOptions, "the mock sets options other than a single CallBase = true in its initializer");
        }

        SeparatedSyntaxList<ArgumentSyntax> written = creation.ArgumentList?.Arguments ?? default;
        if (model.GetSymbolInfo(creation).Symbol is not IMethodSymbol { Parameters: var parameters }
            || written.Any(a => a.NameColon is not null || a.RefKindKeyword.RawKind != 0))
        {
            return MoqBlocker.Of(MoqReason.ConstructorArguments, "the constructor arguments of the class mock are named or by reference");
        }

        // Mock(params object[]) or Mock(MockBehavior, params object[]) or Mock(MockBehavior); not Mock(Expression<Func<T>>).
        bool behavior = parameters is [{ Type.Name: "MockBehavior" }, ..];
        if (!(parameters is [{ IsParams: true }] || behavior || parameters.Length == 0))
        {
            return MoqBlocker.Of(MoqReason.ConstructorArguments, "the class mock uses a constructor overload other than (params object[]) or (MockBehavior, params object[])");
        }

        if (behavior && BehaviorBlocker(written[0].Expression, model) is { } blocker)
        {
            return blocker;
        }

        int skip = behavior ? 1 : 0;
        if (written.Count == skip + 1 && model.GetTypeInfo(written[skip].Expression).Type is IArrayTypeSymbol)
        {
            return MoqBlocker.Of(MoqReason.ConstructorArguments, "the constructor arguments are passed as an array");
        }

        arguments = [.. written.Skip(skip).Select(a => a.Expression)];
        return null;
    }

    // MockBehavior.Loose/Default is dropped (council round 8, M1); Strict and anything not literal stays Manual.
    public static MoqBlocker? BehaviorBlocker(ExpressionSyntax argument, SemanticModel model)
    {
        if (model.GetSymbolInfo(argument).Symbol is IFieldSymbol { ContainingType: { Name: "MockBehavior", ContainingNamespace.Name: "Moq" } } field)
        {
            return field.Name is "Loose" or "Default" ? null
                : MoqBlocker.Of(MoqReason.StrictBehaviour, "NSubstitute has no strict mocks; a strict mock throws on calls without a setup (council round 8, M1)");
        }

        return MoqBlocker.Of(MoqReason.ConstructorArguments, "the argument is not a literal MockBehavior.Loose/Default; see moq-queue.md row 9");
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
            : SyntaxFactory.ParseTypeName(TypeText(mocked, model, position));
    }

    // The shortest name of the type at position, or its global:: name when that short name binds to something else there:
    // `YesSql.Indexes.IIndexProvider` inside namespace `OrchardCore.Data.Migration` finds `OrchardCore.Data.YesSql` first (corpus B6).
    public static string TypeText(ITypeSymbol type, SemanticModel model, int position)
    {
        string minimal = type.ToMinimalDisplayString(model, position);
        ITypeSymbol? bound = model.GetSpeculativeTypeInfo(position, SyntaxFactory.ParseTypeName(minimal), SpeculativeBindingOption.BindAsTypeOrNamespace).Type;
        return SymbolEqualityComparer.Default.Equals(bound, type)
            ? minimal
            : type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions | SymbolDisplayMiscellaneousOptions.UseSpecialTypes));
    }

    public static ExpressionSyntax SubstituteFor(TypeSyntax type) =>
        SyntaxFactory.ParseExpression($"Substitute.For<{type}>()");

    // A class mock (ClassCreation accepted it) becomes a partial substitute with the same constructor arguments.
    public static ExpressionSyntax SubstituteFor(TypeSyntax type, BaseObjectCreationExpressionSyntax creation, ITypeSymbol mocked, SemanticModel model) =>
        mocked.TypeKind == TypeKind.Class && ClassCreation(creation, model, out IReadOnlyList<ExpressionSyntax> arguments) is null
            ? SyntaxFactory.ParseExpression($"Substitute.ForPartsOf<{type}>({string.Join(", ", arguments.Select(a => a.WithoutTrivia()))})")
            : SubstituteFor(type);
}
