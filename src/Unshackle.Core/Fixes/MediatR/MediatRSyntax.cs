using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR;

/// <summary>Shared Task → ValueTask steps for the MediatR handler patterns.</summary>
public static class MediatRSyntax
{
    /// <summary>The Handle methods in <paramref name="root"/> that implement the MediatR interface with this metadata name (e.g. <c>IRequestHandler`2</c>).</summary>
    public static IEnumerable<MethodDeclarationSyntax> FindHandleMethods(SyntaxNode root, SemanticModel model, string interfaceMetadataName)
    {
        foreach (TypeDeclarationSyntax type in root.DescendantNodesAndSelf().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(type) is not INamedTypeSymbol symbol)
            {
                continue;
            }

            // AllInterfaces: a handler may implement MediatR only through its own marker interface (ICommandHandler<T> : IRequestHandler<T>).
            foreach (INamedTypeSymbol handler in symbol.AllInterfaces.Where(i => IsMediatRType(i, interfaceMetadataName)))
            {
                ISymbol? handle = handler.GetMembers("Handle").FirstOrDefault();
                ISymbol? implementation = handle is null ? null : symbol.FindImplementationForInterfaceMember(handle);
                MethodDeclarationSyntax? declaration = implementation?.DeclaringSyntaxReferences
                    .Select(r => r.GetSyntax())
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.SyntaxTree == root.SyntaxTree && type.Members.Contains(m));
                if (declaration is not null)
                {
                    yield return declaration;
                }
            }
        }
    }

    /// <summary>Task → ValueTask and Task&lt;T&gt; → ValueTask&lt;T&gt;, also namespace-qualified; anything else → null.</summary>
    public static TypeSyntax? ToValueTask(TypeSyntax type) => type switch
    {
        GenericNameSyntax { Identifier.ValueText: "Task" } generic => generic.WithIdentifier(SyntaxFactory.Identifier("ValueTask")),
        IdentifierNameSyntax { Identifier.ValueText: "Task" } name => name.WithIdentifier(SyntaxFactory.Identifier("ValueTask")),
        QualifiedNameSyntax { Right: SimpleNameSyntax right } qualified when ToValueTask(right) is SimpleNameSyntax valueTask =>
            qualified.WithRight(valueTask),
        _ => null,
    };

    /// <summary>The expression body, or every return in the body except those of nested lambdas and local functions; a bare <c>return;</c> gives null.</summary>
    public static IReadOnlyList<ExpressionSyntax?> ReturnedExpressions(MethodDeclarationSyntax method) =>
        method.ExpressionBody is not null
            ? [method.ExpressionBody.Expression]
            : [.. method.Body?.DescendantNodes(n => n is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                .OfType<ReturnStatementSyntax>()
                .Select(r => r.Expression) ?? []];

    /// <summary>True for a member of <c>System.Threading.Tasks.Task</c> with this name, e.g. <c>Task.CompletedTask</c> or <c>Task.FromResult</c>.</summary>
    public static bool IsTaskMember(ExpressionSyntax? expression, SemanticModel model, string name) =>
        expression is not null
        && model.GetSymbolInfo(expression).Symbol is { } symbol
        && symbol.Name == name
        && symbol.ContainingType?.ToDisplayString() == "System.Threading.Tasks.Task";

    /// <summary><c>Task.X</c> → <c>ValueTask.X</c>, keeping trivia.</summary>
    public static MemberAccessExpressionSyntax OnValueTask(MemberAccessExpressionSyntax taskMember) =>
        SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName("ValueTask"),
            taskMember.Name).WithTriviaFrom(taskMember);

    /// <summary>
    /// Async: unchanged. Otherwise every <c>return Task.CompletedTask</c> → <c>ValueTask.CompletedTask</c> and every other returned Task
    /// (<c>Task.Run(...)</c>, <c>Task.Delay(...)</c>) → <c>new ValueTask(expr)</c> (no state machine, a synchronous throw stays synchronous, TAAK-154);
    /// null when something that is not a Task is returned. The return type is left to the caller.
    /// </summary>
    public static MethodDeclarationSyntax? WithCompletedValueTask(MethodDeclarationSyntax method, SemanticModel model)
    {
        if (method.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            return method;
        }

        IReadOnlyList<ExpressionSyntax?> returned = ReturnedExpressions(method);
        if (returned.Any(e => e is null || !IsCompletedTask(e, model) && model.GetTypeInfo(e).Type?.ToDisplayString() != "System.Threading.Tasks.Task"))
        {
            return null;
        }

        return method.ReplaceNodes(
            returned.OfType<ExpressionSyntax>(),
            (original, _) => IsCompletedTask(original, model)
                ? OnValueTask((MemberAccessExpressionSyntax)original)
                : SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.IdentifierName("ValueTask").WithLeadingTrivia(SyntaxFactory.Space),
                    SyntaxFactory.ArgumentList([SyntaxFactory.Argument(original.WithoutTrivia())]),
                    null).WithTriviaFrom(original));
    }

    private static bool IsCompletedTask(ExpressionSyntax expression, SemanticModel model) =>
        expression is MemberAccessExpressionSyntax && IsTaskMember(expression, model, "CompletedTask");

    /// <summary>The response type of a request: <c>IRequest&lt;T&gt;</c> → T, <c>IRequest</c> → Unit (Mediator's IRequest is IRequest&lt;Unit&gt;); null when it is neither.</summary>
    public static string? ResponseOf(ITypeSymbol message, SemanticModel model, int position)
    {
        INamedTypeSymbol? request = message.AllInterfaces.Prepend(message as INamedTypeSymbol)
            .FirstOrDefault(i => i is not null && IsMediatR(i, "IRequest"));
        return request switch
        {
            { TypeArguments: [{ } result] } => result.ToMinimalDisplayString(model, position),
            { TypeArguments: [] } => "Unit",
            _ => null,
        };
    }

    /// <summary>The MediatR interface with this metadata name that <paramref name="type"/> declares itself (not through a base class or interface).</summary>
    public static INamedTypeSymbol? DeclaredInterface(INamedTypeSymbol type, string metadataName) =>
        type.Interfaces.FirstOrDefault(i => i.OriginalDefinition.MetadataName == metadataName
            && i.ContainingNamespace.ToDisplayString().StartsWith("MediatR", StringComparison.Ordinal));

    /// <summary>
    /// Replaces the base-list entry of <paramref name="mediatRInterface"/> with the Mediator base class <paramref name="baseClass"/>, moved to the front
    /// (a base class comes first). Null when the entry is not found or the type already has a base class. <paramref name="original"/> is the type in
    /// the tree of <paramref name="model"/>; <paramref name="current"/> is its rewritten copy with the same base list.
    /// </summary>
    public static TypeDeclarationSyntax? WithBaseClass(
        TypeDeclarationSyntax original, TypeDeclarationSyntax current, INamedTypeSymbol mediatRInterface, TypeSyntax baseClass, SemanticModel model)
    {
        if (original.BaseList is not { } originalList
            || current.BaseList is not { } baseList
            || model.GetDeclaredSymbol(original) is not INamedTypeSymbol { BaseType.SpecialType: SpecialType.System_Object })
        {
            return null;
        }

        int index = originalList.Types.IndexOf(t => SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(t.Type).Type, mediatRInterface));
        if (index < 0)
        {
            return null;
        }

        SeparatedSyntaxList<BaseTypeSyntax> types = baseList.Types.RemoveAt(index)
            .Insert(0, SyntaxFactory.SimpleBaseType(baseClass.WithTriviaFrom(baseList.Types[0].Type)));
        if (index != 0)
        {
            // The old first entry keeps its own place, without the leading trivia it gave to the base class.
            types = types.Replace(types[1], types[1].WithLeadingTrivia(SyntaxFactory.Space));
        }

        types = types.Replace(types[^1], types[^1].WithTrailingTrivia(baseList.Types[^1].GetTrailingTrivia()));
        return current.WithBaseList(baseList.WithTypes(types));
    }

    /// <summary><c>public Task X(...)</c> → <c>protected override &lt;returnType&gt; Handle(...)</c>, keeping <c>async</c> and trivia.</summary>
    public static MethodDeclarationSyntax AsHandleOverride(MethodDeclarationSyntax method, TypeSyntax returnType)
    {
        SyntaxTriviaList leading = method.Modifiers.Count > 0 ? method.Modifiers[0].LeadingTrivia : method.ReturnType.GetLeadingTrivia();
        List<SyntaxToken> modifiers =
        [
            SyntaxFactory.Token(SyntaxKind.ProtectedKeyword).WithLeadingTrivia(leading).WithTrailingTrivia(SyntaxFactory.Space),
            SyntaxFactory.Token(SyntaxKind.OverrideKeyword).WithTrailingTrivia(SyntaxFactory.Space),
            .. method.Modifiers.Where(m => m.IsKind(SyntaxKind.AsyncKeyword)).Select(m => m.WithoutTrivia().WithTrailingTrivia(SyntaxFactory.Space)),
        ];
        return method
            .WithModifiers([.. modifiers])
            .WithReturnType(returnType.WithLeadingTrivia().WithTrailingTrivia(method.ReturnType.GetTrailingTrivia()))
            .WithIdentifier(SyntaxFactory.Identifier("Handle").WithTriviaFrom(method.Identifier));
    }

    /// <summary>Manual for a Handle written as an explicit interface implementation, otherwise null.</summary>
    public static PatternResult? RejectExplicit(MethodDeclarationSyntax method, string todo) =>
        method.ExplicitInterfaceSpecifier is null ? null : PatternResult.Manual($"{method.Identifier.ValueText} is an explicit interface implementation; {todo}");

    /// <summary>Nodes separated by ", ".</summary>
    public static SeparatedSyntaxList<T> CommaList<T>(IReadOnlyList<T> nodes)
        where T : SyntaxNode =>
        SyntaxFactory.SeparatedList(nodes, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space), nodes.Count - 1));

    /// <summary>
    /// Adds <c>where T : <paramref name="constraint"/></c> (e.g. <c>IMessage</c>) when <paramref name="messageType"/> is a type parameter of
    /// <paramref name="type"/> whose constraints do not make it one yet: <paramref name="isMessage"/> says which MediatR interfaces already do after the swap.
    /// </summary>
    public static TypeDeclarationSyntax WithConstraint(TypeDeclarationSyntax type, ITypeSymbol? messageType, string constraint, Func<INamedTypeSymbol, bool> isMessage)
    {
        if (messageType is not ITypeParameterSymbol parameter || parameter.TypeParameterKind != TypeParameterKind.Type)
        {
            return type;
        }

        if (parameter.ConstraintTypes.OfType<INamedTypeSymbol>().SelectMany(t => t.AllInterfaces.Prepend(t)).Any(isMessage))
        {
            return type;
        }

        TypeConstraintSyntax message = SyntaxFactory.TypeConstraint(SyntaxFactory.IdentifierName(constraint));
        TypeParameterConstraintClauseSyntax? clause = type.ConstraintClauses.FirstOrDefault(c => c.Name.Identifier.ValueText == parameter.Name);
        if (clause is null)
        {
            TypeParameterConstraintClauseSyntax added = SyntaxFactory.TypeParameterConstraintClause(SyntaxFactory.IdentifierName(parameter.Name))
                .WithConstraints(SyntaxFactory.SingletonSeparatedList<TypeParameterConstraintSyntax>(message));
            return AddClause(type, added);
        }

        if (clause.Constraints.Any(c => c is TypeConstraintSyntax { Type: IdentifierNameSyntax name } && name.Identifier.ValueText == constraint))
        {
            return type;
        }

        // new() must stay last.
        List<TypeParameterConstraintSyntax> constraints = [.. clause.Constraints.Select(c => c.WithoutTrailingTrivia())];
        int index = constraints.FindIndex(c => c is ConstructorConstraintSyntax);
        constraints.Insert(index < 0 ? constraints.Count : index, message);
        constraints[^1] = constraints[^1].WithTrailingTrivia(clause.Constraints.Last().GetTrailingTrivia());
        return type.ReplaceNode(clause, clause.WithConstraints(CommaList(constraints)));
    }

    /// <summary>True for a type of namespace MediatR (or MediatR.Pipeline) with one of these names.</summary>
    public static bool IsMediatR(INamedTypeSymbol type, params string[] names) =>
        names.Contains(type.Name, StringComparer.Ordinal) && type.ContainingNamespace.ToDisplayString().StartsWith("MediatR", StringComparison.Ordinal);

    // where T : X on its own line, one level deeper than the type declaration.
    private static TypeDeclarationSyntax AddClause(TypeDeclarationSyntax type, TypeParameterConstraintClauseSyntax clause)
    {
        SyntaxTriviaList indentation = [.. type.GetLeadingTrivia().Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse()];
        SyntaxToken last = type.ConstraintClauses.LastOrDefault()?.GetLastToken() ?? type.BaseList?.GetLastToken() ?? type.TypeParameterList!.GetLastToken();
        SyntaxTrivia endOfLine = last.TrailingTrivia.FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        clause = clause
            .WithWhereKeyword(SyntaxFactory.Token(SyntaxKind.WhereKeyword).WithLeadingTrivia(indentation.Add(SyntaxFactory.Whitespace("    "))).WithTrailingTrivia(SyntaxFactory.Space))
            .WithColonToken(SyntaxFactory.Token(SyntaxKind.ColonToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space))
            .WithTrailingTrivia(last.TrailingTrivia);
        TypeDeclarationSyntax withoutTrailing = type.ReplaceToken(last, last.WithTrailingTrivia(endOfLine.IsKind(SyntaxKind.None) ? SyntaxFactory.LineFeed : endOfLine));
        return withoutTrailing.WithConstraintClauses(withoutTrailing.ConstraintClauses.Add(clause));
    }

    private static bool IsMediatRType(INamedTypeSymbol type, string metadataName) =>
        type.OriginalDefinition.MetadataName == metadataName
        && type.ContainingNamespace.ToDisplayString() == "MediatR";
}
