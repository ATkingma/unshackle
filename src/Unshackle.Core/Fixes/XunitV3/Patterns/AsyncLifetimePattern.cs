using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.XunitV3.Patterns;

/// <summary>
/// <c>Task InitializeAsync()</c>/<c>Task DisposeAsync()</c> of an <c>IAsyncLifetime</c> implementation (and overrides of one) →
/// <c>ValueTask</c>. A non-async body returns <c>default</c> for <c>Task.CompletedTask</c> and <c>new ValueTask(task)</c> otherwise.
/// </summary>
public class AsyncLifetimePattern : IPattern
{
    private const string LifetimeInterface = "Xunit.IAsyncLifetime";

    private static readonly string[] s_methods = ["InitializeAsync", "DisposeAsync"];

    private static readonly string[] s_taskNamespaces = ["System.Threading.Tasks"];

    // Per compilation: every lifetime method (or interface member) used other than directly awaited. Thread-safe and
    // independent of which file asks first.
    private static readonly ConditionalWeakTable<Compilation, HashSet<ISymbol>> s_nonAwaited = new();

    public string Id => "xunit3.async-lifetime";

    public string Description => "IAsyncLifetime Task InitializeAsync/DisposeAsync → ValueTask";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .Where(m => s_methods.Contains(m.Identifier.ValueText) && m.ParameterList.Parameters.Count == 0)
            .Where(m => model.GetDeclaredSymbol(m) is IMethodSymbol method && LifetimeMember(method) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is not MethodDeclarationSyntax declaration || model.GetDeclaredSymbol(declaration) is not IMethodSymbol method
            || LifetimeMember(method) is not IMethodSymbol member)
        {
            return PatternResult.Manual(XunitV3Reason.UnsupportedShape, "unexpected node.");
        }

        INamedTypeSymbol type = method.ContainingType;
        if (type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_IDisposable || i.ToDisplayString() == "System.IAsyncDisposable"))
        {
            return PatternResult.Manual(XunitV3Reason.DualDisposal,
                $"{type.Name} implements IAsyncLifetime and IDisposable or IAsyncDisposable. In v3 IAsyncLifetime extends IAsyncDisposable and xUnit then calls only DisposeAsync, not Dispose; merge the disposal by hand.");
        }

        HashSet<ISymbol> nonAwaited = s_nonAwaited.GetValue(model.Compilation, NonAwaited);
        if (nonAwaited.Contains(method.OriginalDefinition) || nonAwaited.Contains(member.OriginalDefinition))
        {
            return PatternResult.Manual(XunitV3Reason.LifetimeResultUsed,
                $"{type.Name}.{method.Name} is used as a Task somewhere (not directly awaited); in v3 it returns ValueTask. Change that use by hand.");
        }

        if (ValueTaskType(declaration.ReturnType) is not TypeSyntax valueTask)
        {
            return PatternResult.Manual(XunitV3Reason.UnsupportedShape, $"the return type {declaration.ReturnType} is not written as Task.");
        }

        MethodDeclarationSyntax rewritten = declaration;
        if (!declaration.Modifiers.Any(SyntaxKind.AsyncKeyword))
        {
            List<ExpressionSyntax> returned = [.. Returned(declaration)];
            rewritten = rewritten.ReplaceNodes(returned, (original, _) => Wrap(original, model));
        }

        return PatternResult.Fixed(rewritten.WithReturnType(valueTask.WithTriviaFrom(declaration.ReturnType)), s_taskNamespaces);
    }

    // The IAsyncLifetime member this method implements, directly or as an override of an implementation; null when none.
    private static IMethodSymbol? LifetimeMember(IMethodSymbol method)
    {
        if (method.ReturnType.ToDisplayString() != "System.Threading.Tasks.Task")
        {
            return null;
        }

        for (IMethodSymbol? current = method; current is not null; current = current.OverriddenMethod)
        {
            INamedTypeSymbol? lifetime = current.ContainingType.AllInterfaces
                .FirstOrDefault(i => i.ToDisplayString() == LifetimeInterface && XunitV3Assemblies.IsFrom(i, XunitV3Assemblies.Core));
            string name = current.ExplicitInterfaceImplementations.FirstOrDefault()?.Name ?? current.Name;
            if (lifetime?.GetMembers(name).OfType<IMethodSymbol>().FirstOrDefault() is IMethodSymbol member
                && SymbolEqualityComparer.Default.Equals(current.ContainingType.FindImplementationForInterfaceMember(member), current))
            {
                return member;
            }
        }

        return null;
    }

    private static TypeSyntax? ValueTaskType(TypeSyntax returnType) => returnType switch
    {
        IdentifierNameSyntax { Identifier.ValueText: "Task" } => SyntaxFactory.IdentifierName("ValueTask"),
        QualifiedNameSyntax { Right: IdentifierNameSyntax { Identifier.ValueText: "Task" } } qualified => qualified.WithRight(SyntaxFactory.IdentifierName("ValueTask")),
        _ => null,
    };

    // The expressions the method returns: its expression body or its return statements, not those of lambdas or local functions.
    private static IEnumerable<ExpressionSyntax> Returned(MethodDeclarationSyntax declaration)
    {
        if (declaration.ExpressionBody is { } arrow)
        {
            return [arrow.Expression];
        }

        return declaration.Body is null
            ? []
            : declaration.Body.DescendantNodes(n => n is not AnonymousFunctionExpressionSyntax and not LocalFunctionStatementSyntax)
                .OfType<ReturnStatementSyntax>().Select(r => r.Expression).OfType<ExpressionSyntax>();
    }

    private static ExpressionSyntax Wrap(ExpressionSyntax expression, SemanticModel model)
    {
        if (model.GetSymbolInfo(expression).Symbol is IPropertySymbol { Name: "CompletedTask" } property
            && property.ContainingType.ToDisplayString() == "System.Threading.Tasks.Task")
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression).WithTriviaFrom(expression);
        }

        return SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName("ValueTask").WithLeadingTrivia(SyntaxFactory.Space))
            .WithArgumentList(SyntaxFactory.ArgumentList([SyntaxFactory.Argument(expression.WithoutTrivia())]))
            .WithTriviaFrom(expression);
    }

    // Every lifetime method or IAsyncLifetime member that the compilation names other than as `await x.M()` (or `await x.M().ConfigureAwait(..)`).
    private static HashSet<ISymbol> NonAwaited(Compilation compilation)
    {
        var symbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (SimpleNameSyntax name in tree.GetRoot().DescendantNodes().OfType<SimpleNameSyntax>()
                .Where(n => s_methods.Contains(n.Identifier.ValueText) && !IsAwaited(n)))
            {
                if (model.GetSymbolInfo(name).Symbol is IMethodSymbol method)
                {
                    symbols.Add(method.OriginalDefinition);
                }
            }
        }

        return symbols;
    }

    private static bool IsAwaited(SimpleNameSyntax name)
    {
        SyntaxNode callee = name.Parent is MemberAccessExpressionSyntax access && access.Name == name ? access : name;
        if (callee.Parent is not InvocationExpressionSyntax invocation || invocation.Expression != callee)
        {
            return false;
        }

        SyntaxNode result = invocation.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConfigureAwait", Parent: InvocationExpressionSyntax configured }
            ? configured
            : invocation;
        while (result.Parent is ParenthesizedExpressionSyntax parenthesized)
        {
            result = parenthesized;
        }

        return result.Parent is AwaitExpressionSyntax;
    }
}
