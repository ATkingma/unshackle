using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;

namespace Unshackle.Core;

// TAAK-298: the pre-filter. A file can only get a finding or a change when its text names the old library: a type, static member,
// extension method or namespace of an assembly of the old library, a swapped type, a removed extension method, or a declaration of the solution
// whose signature (base list, type, parameters, constraints, attributes) names one of those, transitively. Everything that
// has the old library's type in a file starts at such a name in that file (a type, a static or extension call, a member declared
// elsewhere whose signature names one),
// so a file without any of them is left alone without a semantic model. Text is matched as identifiers over the whole file,
// comments and inactive #if code included: a false hit only costs time.
internal static partial class OldLibraryFiles
{
    // The files (by path, as the Fixer groups them) that may name the old library.
    public static async Task<HashSet<string>> FindAsync(
        Solution solution, IFix fix, IEnumerable<IGrouping<string, DocumentId>> files, CancellationToken cancellationToken)
    {
        HashSet<string> names = await LibraryNamesAsync(solution, fix, cancellationToken);
        List<OldLibraryFile> scanned = [];
        foreach (IGrouping<string, DocumentId> file in files)
        {
            Document document = solution.GetDocument(file.First())!;
            string text = (await document.GetTextAsync(cancellationToken)).ToString();
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            scanned.Add(new OldLibraryFile
            {
                Path = file.Key,
                // A unicode escape can spell any identifier: such a file is always checked.
                Escaped = text.Contains("\\u", StringComparison.Ordinal) || text.Contains("\\U", StringComparison.Ordinal),
                Identifiers = [.. Identifier().Matches(text).Select(m => m.Value)],
                Declarations = [.. Declarations(root)],
            });
        }

        // The declarations whose signature names the old library add their names, until nothing is added: a worklist over an
        // index from each signature identifier to its declarations, so every declaration is looked at once.
        Dictionary<string, List<OldLibraryDeclaration>> bySignature = new(StringComparer.Ordinal);
        foreach (OldLibraryDeclaration declaration in scanned.SelectMany(f => f.Declarations))
        {
            foreach (string identifier in declaration.Signature)
            {
                if (!bySignature.TryGetValue(identifier, out List<OldLibraryDeclaration>? list))
                {
                    bySignature[identifier] = list = [];
                }

                list.Add(declaration);
            }
        }

        Queue<string> pending = new(names);
        HashSet<OldLibraryDeclaration> reached = [];
        while (pending.TryDequeue(out string? name))
        {
            foreach (OldLibraryDeclaration declaration in bySignature.GetValueOrDefault(name) ?? [])
            {
                if (reached.Add(declaration))
                {
                    foreach (string declared in declaration.Names.Where(names.Add))
                    {
                        pending.Enqueue(declared);
                    }
                }
            }
        }

        return [.. scanned.Where(f => f.Escaped || f.Identifiers.Overlaps(names)).Select(f => f.Path)];
    }

    // Names of the old library: every type and member of each assembly that declares a type in a removed namespace
    // (AddMediatR lives in Microsoft.Extensions.DependencyInjection), the namespaces' parts, and what the fix lists.
    private static async Task<HashSet<string>> LibraryNamesAsync(Solution solution, IFix fix, CancellationToken cancellationToken)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (string ns in fix.RemovedNamespaces)
        {
            names.UnionWith(ns.Split('.'));
        }

        foreach (string type in fix.SwappedTypes)
        {
            string simple = type[(type.LastIndexOf('.') + 1)..];
            names.Add(simple.Split('`')[0]);
        }

        names.UnionWith(fix.RemovedExtensionMethods);
        HashSet<string> removed = [.. fix.RemovedNamespaces];
        HashSet<IAssemblySymbol> seen = new(SymbolEqualityComparer.Default);
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp))
        {
            if (await project.GetCompilationAsync(cancellationToken) is not Compilation compilation)
            {
                continue;
            }

            // Council round 12, G9: the old library's assemblies by name; the removed namespaces below are the fallback.
            foreach (IAssemblySymbol assembly in compilation.SourceModule.ReferencedAssemblySymbols
                .Where(a => fix.OldLibraryAssemblies.Contains(a.Name, StringComparer.OrdinalIgnoreCase)))
            {
                if (seen.Add(assembly))
                {
                    AddAssembly(assembly.GlobalNamespace, names);
                }
            }

            foreach (INamespaceSymbol ns in RemovedNamespaceSymbols(compilation.GlobalNamespace, removed))
            {
                foreach (INamedTypeSymbol type in ns.GetTypeMembers())
                {
                    if (!SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly) && seen.Add(type.ContainingAssembly))
                    {
                        AddAssembly(type.ContainingAssembly.GlobalNamespace, names);
                    }
                }
            }
        }

        return names;
    }

    private static IEnumerable<INamespaceSymbol> RemovedNamespaceSymbols(INamespaceSymbol scope, HashSet<string> removed)
    {
        foreach (INamespaceSymbol ns in scope.GetNamespaceMembers())
        {
            if (removed.Contains(ns.ToDisplayString()))
            {
                yield return ns;
            }

            foreach (INamespaceSymbol inner in RemovedNamespaceSymbols(ns, removed))
            {
                yield return inner;
            }
        }
    }

    private static void AddAssembly(INamespaceSymbol ns, HashSet<string> names)
    {
        foreach (INamespaceSymbol inner in ns.GetNamespaceMembers())
        {
            names.Add(inner.Name);
            AddAssembly(inner, names);
        }

        foreach (INamedTypeSymbol type in ns.GetTypeMembers())
        {
            AddType(type, names);
        }
    }

    private static void AddType(INamedTypeSymbol type, HashSet<string> names)
    {
        names.Add(type.Name);
        if (type.Name.EndsWith("Attribute", StringComparison.Ordinal))
        {
            names.Add(type.Name[..^"Attribute".Length]);
        }

        foreach (ISymbol member in type.GetMembers())
        {
            if (member is INamedTypeSymbol nested)
            {
                AddType(nested, names);
            }
            // Only static members (reachable through `using static` without the type's name) and extension methods: an instance
            // member is called on a value whose origin is named in the file already. Every member would let `Value` or `Name` in.
            else if (member.CanBeReferencedByName && (member.IsStatic || member is IMethodSymbol { IsExtensionMethod: true }))
            {
                names.Add(member.Name);
            }
        }
    }

    // Declarations other files can reach: types (with their primary constructor), members, enum members and using aliases.
    // The names of a member go to its type too: `this`, `base` and inheritance reach it from another file.
    private static IEnumerable<OldLibraryDeclaration> Declarations(SyntaxNode root)
    {
        foreach (SyntaxNode node in root.DescendantNodes(n => n is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax or TypeDeclarationSyntax or EnumDeclarationSyntax))
        {
            switch (node)
            {
                case UsingDirectiveSyntax { Alias: { } alias } directive:
                    yield return Declare(directive, [alias.Name.Identifier.ValueText]);
                    break;
                case TypeDeclarationSyntax { ParameterList: { } parameters } record:
                    // A primary constructor's parameters of a record are properties too.
                    yield return Declare(record, [record.Identifier.ValueText, .. parameters.Parameters.Select(p => p.Identifier.ValueText)], skipMembers: true);
                    break;
                case BaseTypeDeclarationSyntax type:
                    yield return Declare(type, [type.Identifier.ValueText], skipMembers: true);
                    break;
                case DelegateDeclarationSyntax @delegate:
                    yield return Declare(@delegate, [@delegate.Identifier.ValueText]);
                    break;
                case MemberDeclarationSyntax member when member.Parent is BaseTypeDeclarationSyntax owner:
                    yield return Declare(member, [.. MemberNames(member), owner.Identifier.ValueText]);
                    break;
                case MemberDeclarationSyntax member:
                    yield return Declare(member, [.. MemberNames(member)]);
                    break;
            }
        }
    }

    private static IEnumerable<string> MemberNames(MemberDeclarationSyntax member) => member switch
    {
        MethodDeclarationSyntax m => [m.Identifier.ValueText],
        PropertyDeclarationSyntax p => [p.Identifier.ValueText],
        EventDeclarationSyntax e => [e.Identifier.ValueText],
        BaseFieldDeclarationSyntax f => f.Declaration.Variables.Select(v => v.Identifier.ValueText),
        EnumMemberDeclarationSyntax e => [e.Identifier.ValueText],
        _ => [],
    };

    // The signature is the declaration without its bodies and initializers (and, for a type, without its members).
    private static OldLibraryDeclaration Declare(SyntaxNode node, IReadOnlyList<string> names, bool skipMembers = false) => new()
    {
        Names = names,
        Signature = [.. node.DescendantTokens(n => n == node || !(n is BlockSyntax or ArrowExpressionClauseSyntax or EqualsValueClauseSyntax
                || (skipMembers && n is MemberDeclarationSyntax)))
            .Where(t => t.IsKind(SyntaxKind.IdentifierToken))
            .Select(t => t.ValueText)],
    };

    [GeneratedRegex(@"[\p{L}\p{Nl}_][\p{L}\p{Nl}\p{Nd}\p{Mn}\p{Mc}\p{Pc}\p{Cf}]*")]
    private static partial Regex Identifier();
}
