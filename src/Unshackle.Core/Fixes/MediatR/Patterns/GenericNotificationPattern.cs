using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

/// <summary>
/// A generic notification (<c>DomainEventNotification&lt;T&gt; : INotification</c>) breaks Mediator's source generator (open <c>T</c> in
/// Mediator.g.cs). It becomes abstract with one concrete subclass per closed type in the solution; handlers of <c>G&lt;X&gt;</c> get the
/// subclass through Mediator's polymorphic dispatch (TAAK-192). <c>new G&lt;X&gt;(...)</c> and <c>typeof(G&lt;&gt;).MakeGenericType(x)</c> follow.
/// </summary>
public class GenericNotificationPattern : ISolutionPattern
{
    public string Id => "mediatr.generic-notification";

    public string Description =>
        "class G<T> : INotification → abstract G<T> + class GOfX : G<X> per closed type; new G<X>(...) → new GOfX(...), typeof(G<>).MakeGenericType(x) → GTypes.Close(x)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            SyntaxNode? found = node switch
            {
                TypeDeclarationSyntax type when model.GetDeclaredSymbol(type) is INamedTypeSymbol symbol && IsCandidate(symbol, model.Compilation) => type,
                BaseObjectCreationExpressionSyntax creation when CreatedCandidate(creation, model) is not null && !InCandidate(creation, model) => creation,
                TypeOfExpressionSyntax typeOf when UnboundCandidate(typeOf, model) is not null && !InCandidate(typeOf, model) => MakeGenericTypeCall(typeOf) ?? (SyntaxNode)typeOf,
                _ => null,
            };
            if (found is not null)
            {
                yield return found;
            }
        }
    }

    // Without the solution only the own project's closed types are known.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        node is TypeDeclarationSyntax type ? RewriteDeclaration(type, model, [model.Compilation]) : RewriteUse(node, model);

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        if (node is not TypeDeclarationSyntax type)
        {
            return RewriteUse(node, model);
        }

        var compilations = new List<Compilation>();
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp))
        {
            if (await project.GetCompilationAsync(cancellationToken) is Compilation compilation)
            {
                compilations.Add(compilation);
            }
        }

        return RewriteDeclaration(type, model, compilations);
    }

    // Same check in every file: the declaration and its uses must agree without seeing each other's result.
    // Base types of every class in a compilation, as metadata keys.
    private static readonly ConditionalWeakTable<Compilation, HashSet<string>> s_baseTypes = new();

    // A notification with subclasses in the project at hand (kgrzybek's DomainNotificationBase<T>) is left to TAAK-193 (only abstract).
    private static bool IsCandidate(INamedTypeSymbol type, Compilation compilation) =>
        IsCandidateShape(type) && !s_baseTypes.GetValue(compilation, BaseTypes).Contains(MetadataKey(type));

    private static HashSet<string> BaseTypes(Compilation compilation) =>
        new(compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<INamedTypeSymbol>()
            .Select(t => t.BaseType).OfType<INamedTypeSymbol>().Select(b => MetadataKey(b.OriginalDefinition)), StringComparer.Ordinal);

    private static bool IsCandidateShape(INamedTypeSymbol type) =>
        type is { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false, IsGenericType: true, ContainingType: null, TypeParameters.Length: 1 }
        && SymbolEqualityComparer.Default.Equals(type, type.OriginalDefinition)
        && type.Locations.All(l => l.IsInSource)
        && type.AllInterfaces.Any(i => MediatRSyntax.IsMediatR(i, "INotification"))
        && !type.AllInterfaces.Any(i => MediatRSyntax.IsMediatR(i, "IRequest", "IBaseRequest", "IStreamRequest"));

    private static INamedTypeSymbol? CreatedCandidate(BaseObjectCreationExpressionSyntax creation, SemanticModel model) =>
        model.GetTypeInfo(creation).Type is INamedTypeSymbol type && IsCandidate(type.OriginalDefinition, model.Compilation) ? type : null;

    private static INamedTypeSymbol? UnboundCandidate(TypeOfExpressionSyntax typeOf, SemanticModel model) =>
        model.GetTypeInfo(typeOf.Type).Type is INamedTypeSymbol { IsUnboundGenericType: true } type && IsCandidate(type.OriginalDefinition, model.Compilation)
            ? type.OriginalDefinition
            : null;

    // Code inside the notification itself goes along with the declaration (one finding, no overlap).
    private static bool InCandidate(SyntaxNode node, SemanticModel model) =>
        node.Ancestors().OfType<TypeDeclarationSyntax>().Any(t => model.GetDeclaredSymbol(t) is INamedTypeSymbol s && IsCandidate(s, model.Compilation));

    private static InvocationExpressionSyntax? MakeGenericTypeCall(TypeOfExpressionSyntax typeOf) =>
        typeOf.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "MakeGenericType" } member && member.Expression == typeOf
            && member.Parent is InvocationExpressionSyntax { ArgumentList.Arguments.Count: 1 } invocation
            ? invocation
            : null;

    private static PatternResult RewriteUse(SyntaxNode node, SemanticModel model)
    {
        switch (node)
        {
            case BaseObjectCreationExpressionSyntax creation when CreatedCandidate(creation, model) is INamedTypeSymbol created:
                if (created.TypeArguments[0] is not INamedTypeSymbol { IsGenericType: false, TypeKind: not TypeKind.Error } closed)
                {
                    return PatternResult.Manual(
                        $"new {created.Name}<{created.TypeArguments[0].Name}>(...) is generic code; Mediator's source generator needs one concrete notification per closed type. Create the concrete {created.Name}Of... type by hand.");
                }

                TypeSyntax subclass = Retarget((creation as ObjectCreationExpressionSyntax)?.Type, created.OriginalDefinition, SubclassName(created.OriginalDefinition, closed), model, creation.SpanStart);
                return PatternResult.Fixed(SyntaxFactory.ObjectCreationExpression(subclass.WithTrailingTrivia(), creation.ArgumentList ?? SyntaxFactory.ArgumentList(), creation.Initializer)
                    .WithNewKeyword(SyntaxFactory.Token(SyntaxKind.NewKeyword).WithTrailingTrivia(SyntaxFactory.Space)));
            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: TypeOfExpressionSyntax typeOf } } invocation
                when UnboundCandidate(typeOf, model) is INamedTypeSymbol notification:
                if (ConstraintType(notification) is null)
                {
                    return PatternResult.Manual(
                        $"typeof({notification.Name}<>).MakeGenericType(...) needs every closed type up front, and {notification.TypeParameters[0].Name} has no class or interface constraint to find them; publish concrete notifications by hand.");
                }

                TypeSyntax helper = Retarget(typeOf.Type, notification, TypesName(notification), model, typeOf.SpanStart);
                return PatternResult.Fixed(SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, helper, SyntaxFactory.IdentifierName("Close")), invocation.ArgumentList));
            case TypeOfExpressionSyntax typeOfOnly when UnboundCandidate(typeOfOnly, model) is INamedTypeSymbol open:
                return PatternResult.Manual(
                    $"typeof({open.Name}<>) is used other than in MakeGenericType(x); {open.Name}<T> becomes abstract with one concrete subclass per closed type. Rewrite this use by hand.");
            default:
                return PatternResult.Manual("No generic notification found here.");
        }
    }

    private static PatternResult RewriteDeclaration(TypeDeclarationSyntax type, SemanticModel model, IReadOnlyList<Compilation> compilations)
    {
        var notification = (INamedTypeSymbol)model.GetDeclaredSymbol(type)!;
        string name = notification.Name;
        if (type.Modifiers.Any(SyntaxKind.PartialKeyword) || type.Modifiers.Any(SyntaxKind.FileKeyword))
        {
            return PatternResult.Manual($"{name} is partial or file-local; Mediator's source generator does not support generic messages. Write one concrete notification per closed type by hand.");
        }

        if (type.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>().Any(c => CreatedCandidate(c, model) is not null)
            || type.DescendantNodes().OfType<TypeOfExpressionSyntax>().Any(t => UnboundCandidate(t, model) is not null))
        {
            return PatternResult.Manual($"{name}<T> creates or reflects on itself; it becomes abstract with one concrete subclass per closed type. Rewrite that code by hand.");
        }

        NotificationClosedTypes found = Scan(notification, compilations);
        if (found.Subclass is not null)
        {
            return PatternResult.Manual($"{name}<T> already has a subclass ({found.Subclass}); make {name}<T> abstract and give every closed type its own concrete subclass by hand.");
        }

        List<string> keys = [.. found.Arguments];
        if (found.MakesGenericType)
        {
            if (ConstraintType(notification) is null)
            {
                return PatternResult.Manual($"typeof({name}<>).MakeGenericType(...) needs every closed type, and {notification.TypeParameters[0].Name} has no class or interface constraint to find them.");
            }

            keys.AddRange(found.Constrained);
        }

        var closedTypes = new List<INamedTypeSymbol>();
        foreach (string key in keys.Distinct(StringComparer.Ordinal))
        {
            if (model.Compilation.GetTypeByMetadataName(key) is not INamedTypeSymbol closed || !model.Compilation.IsSymbolAccessibleWithin(closed, model.Compilation.Assembly))
            {
                return PatternResult.Manual($"{name}<{key}> is used, but {key} is not visible from {model.Compilation.AssemblyName}, where {name}<T> lives; its concrete subclass cannot be written there.");
            }

            closedTypes.Add(closed);
        }

        if (closedTypes.Count == 0)
        {
            return PatternResult.Manual($"{name}<T> is never closed in the solution; nothing to generate. Remove it or write the concrete notifications by hand.");
        }

        closedTypes.Sort((a, b) => string.CompareOrdinal(SubclassName(notification, a), SubclassName(notification, b)));
        string? clash = closedTypes.GroupBy(c => c.Name, StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1)?.Key
            ?? closedTypes.Select(c => SubclassName(notification, c)).Append(TypesName(notification))
                .FirstOrDefault(n => model.Compilation.GetTypeByMetadataName(Prefix(notification) + n) is not null);
        if (clash is not null)
        {
            return PatternResult.Manual($"The generated name for {name}<T> clashes ({clash}); write the concrete notifications by hand.");
        }

        List<(string Text, IMethodSymbol? Constructor)> constructors = [];
        foreach (IMethodSymbol constructor in notification.InstanceConstructors)
        {
            if (constructor.DeclaredAccessibility == Accessibility.Private || (constructor.IsImplicitlyDeclared && constructor.Parameters.Length > 0))
            {
                continue; // a record's copy constructor is implicit, a private one is unreachable.
            }

            constructors.Add((ConstructorAccess(constructor), constructor.IsImplicitlyDeclared ? null : constructor));
        }

        if (constructors.Count == 0)
        {
            return PatternResult.Manual($"{name}<T> has only private constructors; a subclass cannot call them.");
        }

        string eol = type.SyntaxTree.GetText().ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string indent = new([.. type.GetLeadingTrivia().Reverse().TakeWhile(t => !t.IsKind(SyntaxKind.EndOfLineTrivia)).Reverse()
            .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).SelectMany(t => t.ToString())]);
        string access = notification.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public ? "public " : "internal ";
        string keyword = type is RecordDeclarationSyntax ? "record" : "class";
        int position = type.SpanStart;
        // A blank line between the types; the last type of a file often has no line break after it (SA1513).
        string separator = type.GetTrailingTrivia().Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) ? eol : eol + eol;

        List<SyntaxNode> nodes = [MakeAbstract(type)];
        foreach (INamedTypeSymbol closed in closedTypes)
        {
            string closedText = closed.ToMinimalDisplayString(model, position);
            string subclass = SubclassName(notification, closed);
            string subclassAccess = access == "public " && !IsPublic(closed) ? "internal " : access;
            var text = new StringBuilder();
            text.Append(CultureInfo.InvariantCulture, $"{subclassAccess}{keyword} {subclass} : {name}<{closedText}>{eol}{indent}{{{eol}");
            for (int i = 0; i < constructors.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(eol);
                }

                (string constructorAccess, IMethodSymbol? constructor) = constructors[i];
                (string parameters, string arguments) = constructor is null ? ("()", "") : Parameters(constructor, notification.TypeParameters[0].Name, closedText);
                text.Append(CultureInfo.InvariantCulture, $"{indent}    {constructorAccess} {subclass}{parameters}{eol}");
                text.Append(CultureInfo.InvariantCulture, $"{indent}        : base({arguments}){eol}");
                text.Append(CultureInfo.InvariantCulture, $"{indent}    {{{eol}{indent}    }}{eol}");
            }

            text.Append(CultureInfo.InvariantCulture, $"{indent}}}");
            nodes.Add(Member(text.ToString(), separator));
        }

        if (found.MakesGenericType)
        {
            string typeText = Display(model, "System.Type", position);
            var text = new StringBuilder();
            text.Append(CultureInfo.InvariantCulture, $"{access}static class {TypesName(notification)}{eol}{indent}{{{eol}");
            text.Append(CultureInfo.InvariantCulture, $"{indent}    public static {typeText} Close({typeText} type){eol}{indent}    {{{eol}");
            foreach (INamedTypeSymbol closed in closedTypes)
            {
                text.Append(CultureInfo.InvariantCulture, $"{indent}        if (type == typeof({closed.ToMinimalDisplayString(model, position)})){eol}{indent}        {{{eol}");
                text.Append(CultureInfo.InvariantCulture, $"{indent}            return typeof({SubclassName(notification, closed)});{eol}{indent}        }}{eol}{eol}");
            }

            text.Append(CultureInfo.InvariantCulture, $"{indent}        throw new {Display(model, "System.ArgumentException", position)}(\"No concrete {name} for \" + type + \".\", nameof(type));{eol}");
            text.Append(CultureInfo.InvariantCulture, $"{indent}    }}{eol}{indent}}}");
            nodes.Add(Member(text.ToString(), separator));
        }

        return new FixedResult(nodes, [])
        {
            Warning = $"{name}<X> objects are now {name}Of... subclasses: code that compares GetType() with typeof({name}<X>) behaves differently.",
        };
    }

    private static MemberDeclarationSyntax Member(string text, string separator) =>
        SyntaxFactory.ParseMemberDeclaration(text)!.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(separator));

    private static TypeDeclarationSyntax MakeAbstract(TypeDeclarationSyntax type)
    {
        SyntaxTokenList modifiers = [.. type.Modifiers.Where(m => !m.IsKind(SyntaxKind.SealedKeyword))];
        SyntaxToken @abstract = SyntaxFactory.Token(SyntaxKind.AbstractKeyword).WithTrailingTrivia(SyntaxFactory.Space);
        if (modifiers.Count == 0)
        {
            return type.WithModifiers([@abstract.WithLeadingTrivia(type.Keyword.LeadingTrivia)]).WithKeyword(type.Keyword.WithLeadingTrivia());
        }

        int index = modifiers.Count(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.InternalKeyword);
        return type.WithModifiers(modifiers.Insert(index, @abstract));
    }

    // Parameters with T replaced by the closed type, and the matching base(...) arguments.
    private static (string Parameters, string Arguments) Parameters(IMethodSymbol constructor, string typeParameter, string closedText)
    {
        ParameterListSyntax? list = constructor.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).Select(s => s switch
        {
            ConstructorDeclarationSyntax c => c.ParameterList,
            TypeDeclarationSyntax t => t.ParameterList,
            _ => null,
        }).FirstOrDefault(l => l is not null);
        if (list is null)
        {
            return ("()", "");
        }

        TypeSyntax closedType = SyntaxFactory.ParseTypeName(closedText);
        var substituted = (ParameterListSyntax)list.ReplaceNodes(
            list.DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => n.Identifier.ValueText == typeParameter),
            (_, n) => closedType.WithTriviaFrom(n));
        string arguments = string.Join(", ", constructor.Parameters.Select(p => p.RefKind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            _ => "",
        } + (SyntaxFacts.GetKeywordKind(p.Name) == SyntaxKind.None ? p.Name : "@" + p.Name)));
        return (substituted.WithoutTrivia().ToString(), arguments);
    }

    private static string ConstructorAccess(IMethodSymbol constructor) => constructor.DeclaredAccessibility switch
    {
        Microsoft.CodeAnalysis.Accessibility.Protected => "protected",
        Microsoft.CodeAnalysis.Accessibility.Internal => "internal",
        Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal => "protected internal",
        Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal => "private protected",
        _ => "public",
    };

    private static bool IsPublic(INamedTypeSymbol type) =>
        type.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public && (type.ContainingType is null || IsPublic(type.ContainingType));

    private static string Display(SemanticModel model, string metadataName, int position) =>
        model.Compilation.GetTypeByMetadataName(metadataName)?.ToMinimalDisplayString(model, position) ?? "global::" + metadataName;

    private static ITypeSymbol? ConstraintType(INamedTypeSymbol notification) =>
        notification.TypeParameters[0] is { ConstraintTypes: [INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Interface } constraint], HasConstructorConstraint: false, HasValueTypeConstraint: false }
            ? constraint
            : null;

    private static string SubclassName(INamedTypeSymbol notification, INamedTypeSymbol closed) => $"{notification.Name}Of{closed.Name}";

    private static string TypesName(INamedTypeSymbol notification) => $"{notification.Name}Types";

    private static string Prefix(INamedTypeSymbol notification) =>
        notification.ContainingNamespace.IsGlobalNamespace ? "" : notification.ContainingNamespace.ToDisplayString() + ".";

    private static string MetadataKey(INamedTypeSymbol type) =>
        type.ContainingType is INamedTypeSymbol outer ? MetadataKey(outer) + "+" + type.MetadataName : Prefix(type) + type.MetadataName;

    // The written name of the notification with its last part replaced (keeps N.G<X> → N.GOfX); otherwise by lookup.
    private static TypeSyntax Retarget(TypeSyntax? written, INamedTypeSymbol notification, string target, SemanticModel model, int position)
    {
        IdentifierNameSyntax identifier = SyntaxFactory.IdentifierName(target);
        return written switch
        {
            GenericNameSyntax => identifier,
            QualifiedNameSyntax { Right: GenericNameSyntax } qualified => qualified.WithRight(identifier),
            AliasQualifiedNameSyntax { Name: GenericNameSyntax } alias => alias.WithName(identifier),
            _ when model.LookupNamespacesAndTypes(position, name: notification.Name).Any(s => SymbolEqualityComparer.Default.Equals(s.OriginalDefinition, notification)) => identifier,
            _ => SyntaxFactory.ParseTypeName("global::" + Prefix(notification) + target),
        };
    }

    private static NotificationClosedTypes Scan(INamedTypeSymbol notification, IReadOnlyList<Compilation> compilations)
    {
        string key = MetadataKey(notification);
        ITypeSymbol? constraint = ConstraintType(notification);
        string? constraintKey = constraint is INamedTypeSymbol named ? MetadataKey(named.OriginalDefinition) : null;
        var result = new NotificationClosedTypes();
        foreach (Compilation compilation in compilations)
        {
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel model = compilation.GetSemanticModel(tree);
                foreach (GenericNameSyntax generic in tree.GetRoot().DescendantNodes().OfType<GenericNameSyntax>()
                    .Where(g => g.Identifier.ValueText == notification.Name && g.TypeArgumentList.Arguments.Count == 1))
                {
                    if (model.GetSymbolInfo(generic).Symbol is not INamedTypeSymbol type || MetadataKey(type.OriginalDefinition) != key)
                    {
                        continue;
                    }

                    if (generic.IsUnboundGenericName)
                    {
                        result.MakesGenericType = true;
                    }
                    else if (type.TypeArguments[0] is INamedTypeSymbol { IsGenericType: false, TypeKind: not TypeKind.Error } closed)
                    {
                        result.Arguments.Add(MetadataKey(closed));
                    }
                }
            }

            foreach (INamedTypeSymbol type in compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<INamedTypeSymbol>())
            {
                if (type.BaseType is INamedTypeSymbol baseType && MetadataKey(baseType.OriginalDefinition) == key)
                {
                    result.Subclass ??= type.ToDisplayString();
                }

                if (constraintKey is not null && type is { IsAbstract: false, IsStatic: false, IsGenericType: false, TypeKind: TypeKind.Class }
                    && Inherits(type, constraintKey))
                {
                    result.Constrained.Add(MetadataKey(type));
                }
            }
        }

        return result;
    }

    private static bool Inherits(INamedTypeSymbol type, string key) =>
        type.AllInterfaces.Any(i => MetadataKey(i.OriginalDefinition) == key)
        || EnumerateBases(type).Any(b => MetadataKey(b.OriginalDefinition) == key);

    private static IEnumerable<INamedTypeSymbol> EnumerateBases(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }
}
