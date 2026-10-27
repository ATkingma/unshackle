using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.SpecFlow.Patterns;

/// <summary>
/// A SpecFlow type Reqnroll renamed or moved (<c>ISpecFlowOutputHelper</c> → <c>IReqnrollOutputHelper</c>, <c>TableRow</c> →
/// <c>DataTableRow</c>, <c>Assist.TableHelpers</c> → <c>Reqnroll.TableHelpers</c>), named by its simple name. One without a Reqnroll
/// counterpart is Manual. A name written out in full is <see cref="NamespacePattern"/>'s.
/// </summary>
public class RenamedTypePattern : IPattern
{
    public string Id => "specflow.renamed-type";

    public string Description => "ISpecFlowOutputHelper / SpecFlowContext / TableRow / ... → IReqnrollOutputHelper / ReqnrollContext / DataTableRow / ...";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<SimpleNameSyntax>()
            .Where(n => !(n is IdentifierNameSyntax { IsVar: true })
                && !n.Ancestors().Any(a => a is UsingDirectiveSyntax)
                && !IsQualifiedByNamespace(n, model)
                && Special(n, model) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var name = (SimpleNameSyntax)node;
        string fullName = Special(name, model)!;
        if (SpecFlowNames.RenamedMembers.TryGetValue(fullName, out string? newMember))
        {
            // Council round 21, S11: the type keeps its name; NamespacePattern swaps the using that brings it in.
            return PatternResult.Fixed(name.WithIdentifier(SyntaxFactory.Identifier(newMember).WithTriviaFrom(name.Identifier)));
        }

        if (!SpecFlowNames.RenamedTypes.TryGetValue(fullName, out string? newName))
        {
            return PatternResult.Manual($"{fullName} has no Reqnroll counterpart. Migrate this code by hand.");
        }

        SimpleNameSyntax renamed = name.WithIdentifier(SyntaxFactory.Identifier(SpecFlowNames.SimpleNameOf(newName)));
        // The Fixer adds the using only where the file does not import the namespace yet (also after NamespacePattern's swap).
        return PatternResult.Fixed(renamed, SpecFlowNames.NamespaceOf(newName));
    }

    // The full name of the renamed or removed SpecFlow type this name binds to (a constructor counts as its type), or of the renamed or
    // removed member ("<type>.<member>"); null otherwise.
    private static string? Special(SimpleNameSyntax name, SemanticModel model)
    {
        if (model.GetSymbolInfo(name).Symbol is { } member and not INamedTypeSymbol and not IMethodSymbol { MethodKind: MethodKind.Constructor }
            && ((member as IMethodSymbol)?.ReducedFrom ?? member).ContainingType is INamedTypeSymbol owner && SpecFlowNames.IsOldType(owner))
        {
            string key = SpecFlowNames.FullName(owner.OriginalDefinition) + "." + member.Name;
            return SpecFlowNames.RenamedMembers.ContainsKey(key) || SpecFlowNames.RemovedMembers.Contains(key) ? key : null;
        }

        INamedTypeSymbol? type = model.GetSymbolInfo(name).Symbol switch
        {
            INamedTypeSymbol named => named,
            IMethodSymbol { MethodKind: MethodKind.Constructor } constructor => constructor.ContainingType,
            _ => null,
        };
        if (type is null || !SpecFlowNames.IsOldType(type))
        {
            return null;
        }

        string fullName = SpecFlowNames.FullName(type.OriginalDefinition);
        return SpecFlowNames.RenamedTypes.ContainsKey(fullName) || SpecFlowNames.RemovedTypes.Contains(fullName) ? fullName : null;
    }

    private static bool IsQualifiedByNamespace(SimpleNameSyntax name, SemanticModel model)
    {
        ExpressionSyntax? left = name.Parent switch
        {
            QualifiedNameSyntax qualified when qualified.Right == name => qualified.Left,
            MemberAccessExpressionSyntax access when access.Name == name => access.Expression,
            _ => null,
        };
        return left is not null && model.GetSymbolInfo(left).Symbol is INamespaceSymbol;
    }
}
