using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// A Profile whose constructor only holds CreateMap&lt;A, B&gt;() calls without options → a static class next to it with
/// one B ToB(this A source) per map, copying the members with the same name and type. Anything AutoMapper would do
/// beyond that (conversion, flattening, collections, constructors) is Manual.
/// </summary>
public class CreateMapPattern : IPattern
{
    private const string AutoMapperAssembly = "AutoMapper";

    public string Id => "automapper.create-map";

    public string Description =>
        "Profile with CreateMap<A, B>() without options → static B ToB(this A source) => new() { X = source.X, ... } for the members with the same name and type; mapper.Map is Manual until automapper.map-call";

    // Map and ProjectTo calls are found too: while they run, a removed CreateMap fails at run time, so they make the fix all-or-nothing.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => n switch
        {
            ClassDeclarationSyntax c => model.GetDeclaredSymbol(c) is INamedTypeSymbol type && IsProfile(type)
                && c.Members.OfType<ConstructorDeclarationSyntax>().Any(ctor => ctor.Body is { Statements.Count: > 0 } || ctor.ExpressionBody is not null),
            InvocationExpressionSyntax i => model.GetSymbolInfo(i) is var info
                && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "Map" or "ProjectTo" } method
                && method.ContainingAssembly?.Name == AutoMapperAssembly,
            _ => false,
        });

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is InvocationExpressionSyntax)
        {
            return PatternResult.Manual(
                "AutoMapper's Map/ProjectTo is not migrated yet (automapper.map-call); while it runs, the CreateMap rewrites of this fix stay undone.");
        }

        var profile = (ClassDeclarationSyntax)node;
        if (profile.Parent is not (BaseNamespaceDeclarationSyntax or CompilationUnitSyntax) || profile.TypeParameterList is not null
            || profile.Modifiers.Any(SyntaxKind.PartialKeyword)
            || profile.Members.OfType<ConstructorDeclarationSyntax>().ToList() is not [{ Body: { } body }])
        {
            return PatternResult.Manual($"Profile {profile.Identifier} is nested, generic, partial or has several constructors; write its mapping methods by hand.");
        }

        List<(TypeSyntax Source, TypeSyntax Destination, List<string> Members)> maps = [];
        bool isInternal = false;
        foreach (StatementSyntax statement in body.Statements)
        {
            if (statement is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0 } invocation }
                || invocation.Expression is not GenericNameSyntax { Identifier.ValueText: "CreateMap", TypeArgumentList.Arguments: [{ } source, { } destination] }
                || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly } method)
            {
                return PatternResult.Manual($"'{string.Join(' ', statement.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))}' is not a CreateMap<A, B>() without options; migrate Profile {profile.Identifier} by hand.");
            }

            ITypeSymbol sourceType = method.TypeArguments[0];
            ITypeSymbol destinationType = method.TypeArguments[1];
            if (Problem(sourceType, destinationType, out List<string> members) is string problem)
            {
                return PatternResult.Manual($"CreateMap<{sourceType.Name}, {destinationType.Name}>: {problem}; write the mapping method by hand.");
            }

            Accessibility access = Weakest(Weakest(Effective(sourceType), Effective(destinationType)), Accessibility.Public);
            if (access is not (Accessibility.Public or Accessibility.Internal))
            {
                return PatternResult.Manual($"{sourceType.Name} or {destinationType.Name} is private or protected; an extension method cannot use it.");
            }

            isInternal |= access == Accessibility.Internal;
            maps.Add((source, destination, members));
        }

        string className = profile.Identifier.ValueText + "Extensions";
        if (model.Compilation.GetSymbolsWithName(className, SymbolFilter.Type).Any())
        {
            return PatternResult.Manual($"A type {className} already exists; write the mapping methods of {profile.Identifier} by hand.");
        }

        return PatternResult.Fixed([
            profile.ReplaceNode(body, body.WithStatements([])),
            Extensions(profile, className, isInternal ? "internal" : "public", maps)]);
    }

    // Why the map cannot be written as plain assignments, or null with the destination members AutoMapper writes,
    // each having a source member of the same name and type.
    private static string? Problem(ITypeSymbol source, ITypeSymbol destination, out List<string> members)
    {
        members = [];
        if (destination is not INamedTypeSymbol { IsAbstract: false } named || destination.TypeKind is not (TypeKind.Class or TypeKind.Struct)
            || (destination.TypeKind == TypeKind.Class && !named.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public)))
        {
            return $"{destination.Name} has no public parameterless constructor (AutoMapper would map constructor parameters)";
        }

        List<ISymbol> readable = [.. AllMembers(source).Where(m => m switch
        {
            IPropertySymbol p => !p.IsIndexer && p.GetMethod?.DeclaredAccessibility == Accessibility.Public,
            IFieldSymbol => true,
            _ => false,
        })];
        foreach (ISymbol target in AllMembers(destination).Where(m => m is IPropertySymbol { IsIndexer: false } or IFieldSymbol { IsReadOnly: false, IsConst: false }))
        {
            bool writable = target is IFieldSymbol || ((IPropertySymbol)target).SetMethod?.DeclaredAccessibility == Accessibility.Public;
            List<ISymbol> matches = [.. readable.Where(m => string.Equals(m.Name, target.Name, StringComparison.OrdinalIgnoreCase))];
            if (!writable)
            {
                // AutoMapper fills a get-only member when the source has it (an existing collection, for one).
                if (matches.Count > 0)
                {
                    return $"{destination.Name}.{target.Name} has no public setter but {source.Name} has the member";
                }

                continue;
            }

            if (matches is not [{ } match] || match.Name != target.Name)
            {
                // No member of that exact name: AutoMapper may flatten (Customer.Name → CustomerName), call GetX() or match case-insensitively.
                return $"{destination.Name}.{target.Name} has no member with the same name in {source.Name}";
            }

            ITypeSymbol from = match is IPropertySymbol fp ? fp.Type : ((IFieldSymbol)match).Type;
            ITypeSymbol to = target is IPropertySymbol tp ? tp.Type : ((IFieldSymbol)target).Type;
            if (!SymbolEqualityComparer.Default.Equals(from, to))
            {
                return $"{target.Name} is {from.Name} in {source.Name} but {to.Name} in {destination.Name} (AutoMapper converts)";
            }

            if (to.SpecialType != SpecialType.System_String && (to is IArrayTypeSymbol || to.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable)))
            {
                return $"{target.Name} is a collection (AutoMapper copies it into a new one)";
            }

            members.Add(target.Name);
        }

        return null;
    }

    // Public instance members, the type's own first; a member hidden by a derived one counts once.
    private static IEnumerable<ISymbol> AllMembers(ITypeSymbol type)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (ITypeSymbol? current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (ISymbol member in current.GetMembers().Where(m => !m.IsStatic && !m.IsImplicitlyDeclared && m.DeclaredAccessibility == Accessibility.Public))
            {
                if (seen.Add(member.Name))
                {
                    yield return member;
                }
            }
        }
    }

    private static bool IsProfile(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current is { Name: "Profile", ContainingAssembly.Name: AutoMapperAssembly })
            {
                return true;
            }
        }

        return false;
    }

    // The accessibility a type really has, through its containing types and type arguments.
    private static Accessibility Effective(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => Effective(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Aggregate(
            named.ContainingType is { } outer ? Weakest(named.DeclaredAccessibility, Effective(outer)) : named.DeclaredAccessibility,
            (access, argument) => Weakest(access, Effective(argument))),
        _ => Accessibility.Public,
    };

    private static Accessibility Weakest(Accessibility a, Accessibility b) => (a, b) switch
    {
        (Accessibility.Public, _) => b,
        (_, Accessibility.Public) => a,
        (Accessibility.Internal, Accessibility.Internal) => Accessibility.Internal,
        _ => Accessibility.Private,
    };

    // The static class goes right after the profile, with its indentation and line endings.
    private static MemberDeclarationSyntax Extensions(ClassDeclarationSyntax profile, string className, string access,
        List<(TypeSyntax Source, TypeSyntax Destination, List<string> Members)> maps)
    {
        string indent = profile.GetLeadingTrivia().LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString();
        string eol = profile.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } found
            ? found
            : "\n";
        var text = new StringBuilder();
        text.Append(access).Append(" static class ").Append(className).Append(eol).Append(indent).Append('{').Append(eol);
        foreach ((TypeSyntax source, TypeSyntax destination, List<string> members) in maps)
        {
            if (text[^(eol.Length + 1)] != '{')
            {
                text.Append(eol);
            }

            string name = destination is GenericNameSyntax generic ? generic.Identifier.ValueText : destination.ToString().Split('.')[^1];
            text.Append(indent).Append("    ").Append(access).Append(" static ").Append(destination).Append(" To").Append(name)
                .Append("(this ").Append(source).Append(" source) => new()");
            if (members.Count == 0)
            {
                text.Append(';').Append(eol);
                continue;
            }

            text.Append(eol).Append(indent).Append("    {").Append(eol);
            foreach (string member in members)
            {
                text.Append(indent).Append("        ").Append(member).Append(" = source.").Append(member).Append(',').Append(eol);
            }

            text.Append(indent).Append("    };").Append(eol);
        }

        text.Append(indent).Append('}').Append(eol);
        // The blank line before it is set as trivia: the parser would drop a leading line ending.
        return SyntaxFactory.ParseMemberDeclaration(text.ToString())!.WithLeadingTrivia(SyntaxFactory.EndOfLine(eol), SyntaxFactory.Whitespace(indent));
    }
}
