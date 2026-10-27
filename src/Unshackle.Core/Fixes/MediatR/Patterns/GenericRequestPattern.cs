using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Generics;
using Unshackle.Core.Models;
using static Unshackle.Core.Generics.GenericUses;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

/// <summary>
/// A generic request (dotnet/eShop: <c>IdentifiedCommand&lt;T, R&gt; : IRequest&lt;R&gt;</c>) is invisible to Mediator's source generator, and
/// Mediator dispatches a request on its exact type (a handler of a base type is not used, measured on 3.0.2, TAAK-193). The request becomes
/// abstract with one concrete subclass per closed type in the solution (<c>IdentifiedCommandOfCreateOrderCommandAndBoolean</c>), every closed
/// <c>G&lt;X, Y&gt;</c> is renamed to it (handlers, <c>new</c>, validators and their DI keys), and a concrete handler that gets
/// <c>IRequestHandler&lt;G&lt;X, Y&gt;, Y&gt;</c> from an abstract generic base gets <c>IRequestHandler&lt;Sub, Y&gt;</c> with a Handle that calls the base.
/// A generic request or notification that is only the base of concrete subclasses (kgrzybek: <c>DomainNotificationBase&lt;T&gt;</c>) only becomes abstract.
/// </summary>
public class GenericRequestPattern : ISolutionPattern, ISolutionFinder
{
    public string Id => "mediatr.generic-request";

    public string Description =>
        "class G<T, R> : IRequest<R> → abstract G<T, R> + class GOfXAndY : G<X, Y> per closed type, G<X, Y> → GOfXAndY, inherited handler gets IRequestHandler<GOfXAndY, Y>; "
        + "a generic message that is only a base class → abstract";

    /// <summary>A generic request class this pattern migrates (also when it reports Manual).</summary>
    public static bool IsCandidate(INamedTypeSymbol type) =>
        IsGenericMessage(type)
        && type.AllInterfaces.Any(i => MediatRSyntax.IsMediatR(i, "IRequest", "IBaseRequest"))
        && !type.AllInterfaces.Any(i => MediatRSyntax.IsMediatR(i, "IStreamRequest"));

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) => FindNodes(root, model, [model.Compilation]);

    public async Task<IReadOnlyList<SyntaxNode>> FindAsync(SyntaxNode root, SemanticModel model, Solution solution, CancellationToken cancellationToken) =>
        [.. FindNodes(root, model, await GenericUses.CompilationsAsync(solution, cancellationToken))];

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => RewriteNode(node, model, [model.Compilation]);

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken) =>
        RewriteNode(node, model, node is TypeDeclarationSyntax ? await GenericUses.CompilationsAsync(solution, cancellationToken) : [model.Compilation]);

    private static bool IsGenericMessage(INamedTypeSymbol type) =>
        type is { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false, IsGenericType: true, ContainingType: null }
        && SymbolEqualityComparer.Default.Equals(type, type.OriginalDefinition)
        && type.Locations.All(l => l.IsInSource)
        && type.AllInterfaces.Any(i => MediatRSyntax.IsMediatR(i, "IRequest", "IBaseRequest", "INotification"));

    // Left to GenericNotificationPattern (TAAK-192), except when it is only the base of concrete subclasses.
    private static bool IsOnlySubclassedNotification(INamedTypeSymbol type, IReadOnlyList<Compilation> compilations)
    {
        if (!IsGenericMessage(type) || IsCandidate(type))
        {
            return false;
        }

        GenericRequestUses uses = Scan(type, compilations);
        return uses.Closed.Count == 0 && uses.Subclassed.Count > 0 && uses.Unbound is null && uses.OpenCreation is null;
    }

    private static IEnumerable<SyntaxNode> FindNodes(SyntaxNode root, SemanticModel model, IReadOnlyList<Compilation> compilations)
    {
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            switch (node)
            {
                case TypeDeclarationSyntax type when model.GetDeclaredSymbol(type) is INamedTypeSymbol symbol
                    && (IsCandidate(symbol) || IsOnlySubclassedNotification(symbol, compilations) || InheritedHandlers(symbol).Any()):
                    yield return type;
                    break;
                case GenericNameSyntax generic when Use(generic, model) is not null:
                    yield return generic;
                    break;
            }
        }
    }

    // A closed G<X, Y> to rename: not in G itself and not the base class of an existing subclass.
    private static INamedTypeSymbol? Use(GenericNameSyntax generic, SemanticModel model)
    {
        if (generic.IsUnboundGenericName || model.GetSymbolInfo(generic).Symbol is not INamedTypeSymbol type || !IsCandidate(type.OriginalDefinition)
            || type.TypeArguments.Any(ContainsTypeParameter) || InDeclarationOf(generic, type.OriginalDefinition, model) || IsSubclassBase(generic, type, model))
        {
            return null;
        }

        return type;
    }

    // IRequestHandler<G<X, Y>, Y> of a concrete class that gets it from a base class, with G a generic request.
    private static IEnumerable<INamedTypeSymbol> InheritedHandlers(INamedTypeSymbol type) =>
        type is { TypeKind: TypeKind.Class, IsAbstract: false, IsGenericType: false }
            ? type.AllInterfaces.Where(i => IsHandlerOfClosedCandidate(i) && !type.Interfaces.Contains(i, SymbolEqualityComparer.Default))
            : [];

    private static bool IsHandlerOfClosedCandidate(INamedTypeSymbol handler) =>
        handler.OriginalDefinition.MetadataName is "IRequestHandler`1" or "IRequestHandler`2"
        && MediatRSyntax.IsMediatR(handler, "IRequestHandler")
        && handler.TypeArguments[0] is INamedTypeSymbol message
        && IsCandidate(message.OriginalDefinition)
        && !message.TypeArguments.Any(ContainsTypeParameter);

    private static PatternResult RewriteNode(SyntaxNode node, SemanticModel model, IReadOnlyList<Compilation> compilations)
    {
        if (node is GenericNameSyntax generic)
        {
            return RewriteUse(generic, model);
        }

        var type = (TypeDeclarationSyntax)node;
        var symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(type)!;
        return IsGenericMessage(symbol) ? RewriteDeclaration(type, symbol, model, compilations) : RewriteHandler(type, symbol, model);
    }

    private static PatternResult RewriteUse(GenericNameSyntax generic, SemanticModel model)
    {
        INamedTypeSymbol? type = Use(generic, model);
        if (type is null || SubclassName(type) is not string name)
        {
            return PatternResult.Manual($"{generic} has a type argument that is not a plain named type; write its concrete request by hand.");
        }

        SyntaxNode? member = generic.Ancestors().FirstOrDefault(a => a is ParameterListSyntax or MemberDeclarationSyntax);
        if (member is ParameterListSyntax { Parent: MemberDeclarationSyntax owner } ? IsOverride(owner) : member is MemberDeclarationSyntax declaration && IsOverride(declaration))
        {
            return PatternResult.Manual($"{generic} is part of an override's signature; renaming it to {name} would break the override. Rewrite it by hand.");
        }

        return PatternResult.Fixed(SyntaxFactory.IdentifierName(name).WithTriviaFrom(generic));
    }

    private static bool IsOverride(MemberDeclarationSyntax member) => member.Modifiers.Any(SyntaxKind.OverrideKeyword);

    // GOfXAndY; null when an argument is not a plain named type (G<List<int>>, G<int[]>).
    private static string? SubclassName(INamedTypeSymbol closed) =>
        closed.TypeArguments.All(a => a is INamedTypeSymbol { IsGenericType: false, TypeKind: not TypeKind.Error })
            ? $"{closed.Name}Of{string.Join("And", closed.TypeArguments.Select(a => a.Name))}"
            : null;

    private static PatternResult RewriteDeclaration(TypeDeclarationSyntax type, INamedTypeSymbol message, SemanticModel model, IReadOnlyList<Compilation> compilations)
    {
        string name = message.Name;
        if (type is RecordDeclarationSyntax || type.Modifiers.Any(SyntaxKind.PartialKeyword) || type.Modifiers.Any(SyntaxKind.FileKeyword))
        {
            return PatternResult.Manual($"{name} is a record, partial or file-local; Mediator's source generator does not support generic messages. Write one concrete message per closed type by hand.");
        }

        GenericRequestUses uses = Scan(message, compilations);
        if (uses.Unbound is not null)
        {
            return PatternResult.Manual($"typeof({name}<>) is used ({uses.Unbound}); {name} becomes abstract with one concrete subclass per closed type. Rewrite that use by hand.");
        }

        if (uses.OpenCreation is not null)
        {
            return PatternResult.Manual($"{name}: open type argument in generic code; callers not followed ({uses.OpenCreation}). Mediator needs one concrete request per closed type. Create it by hand.");
        }

        string? both = uses.Closed.Keys.FirstOrDefault(uses.Subclassed.Contains);
        if (both is not null)
        {
            return PatternResult.Manual($"{name}<{both}> is used and already has a subclass; which of the two is sent cannot be told apart. Rewrite it by hand.");
        }

        var closedTypes = new List<INamedTypeSymbol>();
        foreach (IReadOnlyList<ITypeSymbol> arguments in uses.Closed.Values)
        {
            var mapped = new List<ITypeSymbol>();
            foreach (ITypeSymbol argument in arguments)
            {
                if (argument is not INamedTypeSymbol { IsGenericType: false, TypeKind: not TypeKind.Error } named
                    || model.Compilation.GetTypeByMetadataName(MetadataKey(named)) is not INamedTypeSymbol local
                    || !model.Compilation.IsSymbolAccessibleWithin(local, model.Compilation.Assembly))
                {
                    return PatternResult.Manual(
                        $"{name}<{string.Join(", ", arguments.Select(a => a.ToDisplayString()))}> is used, but {argument.ToDisplayString()} is not a plain type visible from {model.Compilation.AssemblyName}, where {name} lives; its concrete subclass cannot be written there.");
                }

                mapped.Add(local);
            }

            closedTypes.Add(message.Construct([.. mapped]));
        }

        closedTypes.Sort((a, b) => string.CompareOrdinal(SubclassName(a), SubclassName(b)));
        string? clash = closedTypes.GroupBy(c => SubclassName(c), StringComparer.Ordinal).FirstOrDefault(g => g.Count() > 1)?.Key
            ?? closedTypes.Select(c => SubclassName(c)!).FirstOrDefault(n => model.Compilation.GetTypeByMetadataName(Prefix(message) + n) is not null);
        if (clash is not null)
        {
            return PatternResult.Manual($"The generated name {clash} for a closed {name} clashes; write the concrete requests by hand.");
        }

        string access = message.DeclaredAccessibility == Accessibility.Public ? "public " : "internal ";
        foreach (INamedTypeSymbol closed in closedTypes)
        {
            bool internalOnly = access == "internal " || closed.TypeArguments.Any(a => !IsPublic((INamedTypeSymbol)a));
            string? elsewhere = internalOnly ? uses.Assemblies.FirstOrDefault(a => a != model.Compilation.AssemblyName) : null;
            if (elsewhere is not null)
            {
                return PatternResult.Manual($"{SubclassName(closed)} can only be internal to {model.Compilation.AssemblyName}, but {elsewhere} uses it; write the concrete request by hand.");
            }
        }

        var constructors = message.InstanceConstructors.Where(c => c.DeclaredAccessibility != Accessibility.Private).ToList();
        if (constructors.Count == 0)
        {
            return PatternResult.Manual($"{name} has only private constructors; a subclass cannot call them.");
        }

        if (constructors.SelectMany(c => c.Parameters).FirstOrDefault(p => p.RefKind != RefKind.None || p.IsParams || p.HasExplicitDefaultValue) is IParameterSymbol special)
        {
            return PatternResult.Manual($"A constructor of {name} has a ref, params or optional parameter ({special.Name}); write the concrete requests by hand.");
        }

        string eol = type.SyntaxTree.GetText().ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string indent = new([.. type.GetLeadingTrivia().Reverse().TakeWhile(t => !t.IsKind(SyntaxKind.EndOfLineTrivia)).Reverse()
            .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).SelectMany(t => t.ToString())]);
        int position = type.SpanStart;
        string separator = type.GetTrailingTrivia().Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) ? eol : eol + eol;

        List<SyntaxNode> nodes = [MakeAbstract(type)];
        foreach (INamedTypeSymbol closed in closedTypes)
        {
            string subclass = SubclassName(closed)!;
            string subclassAccess = closed.TypeArguments.Any(a => !IsPublic((INamedTypeSymbol)a)) ? "internal " : access;
            var text = new StringBuilder();
            text.Append(CultureInfo.InvariantCulture, $"{subclassAccess}class {subclass} : {closed.ToMinimalDisplayString(model, position)}{eol}{indent}{{{eol}");
            List<IMethodSymbol> closedConstructors = [.. closed.InstanceConstructors.Where(c => c.DeclaredAccessibility != Accessibility.Private)];
            for (int i = 0; i < closedConstructors.Count; i++)
            {
                IMethodSymbol constructor = closedConstructors[i];
                if (i > 0)
                {
                    text.Append(eol);
                }

                string parameters = string.Join(", ", constructor.Parameters.Select(p => $"{p.Type.ToMinimalDisplayString(model, position)} {Escape(p.Name)}"));
                string arguments = string.Join(", ", constructor.Parameters.Select(p => Escape(p.Name)));
                text.Append(CultureInfo.InvariantCulture, $"{indent}    {ConstructorAccess(constructor)} {subclass}({parameters}){eol}");
                text.Append(CultureInfo.InvariantCulture, $"{indent}        : base({arguments}){eol}");
                text.Append(CultureInfo.InvariantCulture, $"{indent}    {{{eol}{indent}    }}{eol}");
            }

            text.Append(CultureInfo.InvariantCulture, $"{indent}}}");
            nodes.Add(SyntaxFactory.ParseMemberDeclaration(text.ToString())!.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(separator)));
        }

        return new FixedResult(nodes, [])
        {
            Warning = closedTypes.Count == 0
                ? null
                : $"{name}<X, ...> objects are now {name}Of... subclasses: GetType() (e.g. in log text) and comparisons with typeof({name}<X, ...>) see the subclass.",
        };
    }

    // class C : Base<X, Y> where Base<T, R> : IRequestHandler<G<T, R>, R> → C : Base<X, Y>, IRequestHandler<GOfXAndY, Y> + Handle(GOfXAndY, ct) => base.Handle(...).
    private static PatternResult RewriteHandler(TypeDeclarationSyntax type, INamedTypeSymbol handler, SemanticModel model)
    {
        if (type.BaseList is null)
        {
            return PatternResult.Manual($"{handler.Name} gets its request handler from a base type without a base list; add the concrete request handler by hand.");
        }

        int position = type.SpanStart;
        List<string> interfaces = [];
        List<string> methods = [];
        string eol = type.SyntaxTree.GetText().ToString().Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        string indent = type.Members.FirstOrDefault() is MemberDeclarationSyntax first
            ? new([.. first.GetLeadingTrivia().Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).SelectMany(t => t.ToString())])
            : new string(' ', 4);
        foreach (INamedTypeSymbol inherited in InheritedHandlers(handler))
        {
            var message = (INamedTypeSymbol)inherited.TypeArguments[0];
            if (SubclassName(message) is not string subclass)
            {
                return PatternResult.Manual($"{message.ToDisplayString()} has a type argument that is not a plain named type; add the concrete request handler by hand.");
            }

            // After the request-handler pattern the base's Handle returns ValueTask while the model still sees MediatR's Task interface
            // (the using is swapped last), so it implements nothing: then it is the base class's Handle taking this message.
            IMethodSymbol? implementation = (inherited.GetMembers("Handle").OfType<IMethodSymbol>().FirstOrDefault() is IMethodSymbol handle
                ? handler.FindImplementationForInterfaceMember(handle) as IMethodSymbol
                : null) ?? InheritedHandle(handler, message);
            if (implementation is not { DeclaredAccessibility: Accessibility.Public, ExplicitInterfaceImplementations.Length: 0 }
                || SymbolEqualityComparer.Default.Equals(implementation.ContainingType, handler)
                || handler.GetMembers("Handle").Any(m => m.IsOverride))
            {
                return PatternResult.Manual($"{handler.Name} gets Handle for {message.Name} from a base class in a way this fix does not forward (explicit, overridden or not public); add the concrete request handler by hand.");
            }

            string response = inherited.TypeArguments.Length == 2 ? inherited.TypeArguments[1].ToMinimalDisplayString(model, position) : "Unit";
            interfaces.Add(inherited.TypeArguments.Length == 2 ? $"IRequestHandler<{subclass}, {response}>" : $"IRequestHandler<{subclass}>");
            string request = Escape(implementation.Parameters[0].Name);
            string token = Escape(implementation.Parameters[1].Name);
            methods.Add($"{indent}public ValueTask<{response}> Handle({subclass} {request}, CancellationToken {token}) => base.Handle({request}, {token});");
        }

        BaseListSyntax baseList = type.BaseList;
        SyntaxTriviaList trailing = baseList.Types[^1].GetTrailingTrivia();
        SeparatedSyntaxList<BaseTypeSyntax> types = baseList.Types.Replace(baseList.Types[^1], baseList.Types[^1].WithoutTrailingTrivia());
        foreach (string added in interfaces)
        {
            types = types.Add(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName(added)));
        }

        types = types.Replace(types[^1], types[^1].WithTrailingTrivia(trailing));
        TypeDeclarationSyntax rewritten = type.WithBaseList(baseList.WithTypes(SyntaxFactory.SeparatedList(
            types, Enumerable.Repeat(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space), types.Count - 1))));
        string leading = type.Members.Count > 0 ? eol : "";
        rewritten = rewritten.AddMembers([.. methods.Select(m => SyntaxFactory.ParseMemberDeclaration(m)!
            .WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia(leading + indent))
            .WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(eol)))]);
        return PatternResult.Fixed(rewritten, "Mediator", "System.Threading", "System.Threading.Tasks");
    }

    private static IMethodSymbol? InheritedHandle(INamedTypeSymbol handler, INamedTypeSymbol message)
    {
        for (INamedTypeSymbol? type = handler.BaseType; type is not null; type = type.BaseType)
        {
            if (type.GetMembers("Handle").OfType<IMethodSymbol>().FirstOrDefault(m => m is { IsStatic: false, Parameters.Length: 2 }
                && SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type, message)) is IMethodSymbol handle)
            {
                return handle;
            }
        }

        return null;
    }

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

    private static string Escape(string name) => SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    private static string ConstructorAccess(IMethodSymbol constructor) => constructor.DeclaredAccessibility switch
    {
        Accessibility.Protected => "protected",
        Accessibility.Internal => "internal",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => "public",
    };

    private static bool IsPublic(INamedTypeSymbol type) =>
        type.SpecialType != SpecialType.None
        || (type.DeclaredAccessibility == Accessibility.Public && (type.ContainingType is null || IsPublic(type.ContainingType)));

    // Every use of the generic message in the solution, by the written G<...> names (GenericUses) and by inherited handler interfaces.
    private static GenericRequestUses Scan(INamedTypeSymbol message, IReadOnlyList<Compilation> compilations)
    {
        string key = MetadataKey(message);
        var result = new GenericRequestUses();
        foreach (GenericUse use in GenericUses.Find(message, compilations))
        {
            switch (use.Kind)
            {
                case GenericUseKind.Unbound:
                    result.Unbound ??= use.Where;
                    break;
                case GenericUseKind.SubclassBase:
                    result.Subclassed.Add(ArgumentsKey(use.Constructed));
                    break;
                case GenericUseKind.OpenCreation:
                    result.OpenCreation ??= use.Where;
                    break;
                case GenericUseKind.Closed:
                    result.Closed[ArgumentsKey(use.Constructed)] = use.Constructed.TypeArguments;
                    result.Assemblies.Add(use.AssemblyName);
                    break;
            }
        }

        foreach (Compilation compilation in compilations)
        {
            foreach (INamedTypeSymbol type in compilation.GetSymbolsWithName(_ => true, SymbolFilter.Type).OfType<INamedTypeSymbol>())
            {
                foreach (INamedTypeSymbol inherited in InheritedHandlers(type))
                {
                    var closed = (INamedTypeSymbol)inherited.TypeArguments[0];
                    if (MetadataKey(closed.OriginalDefinition) == key)
                    {
                        result.Closed[ArgumentsKey(closed)] = closed.TypeArguments;
                        result.Assemblies.Add(compilation.AssemblyName ?? "");
                    }
                }
            }
        }

        return result;
    }
}
