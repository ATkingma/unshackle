using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// A Profile whose constructor only holds CreateMap&lt;A, B&gt;() calls without options (a constant MemberList argument only steers
/// AssertConfigurationIsValid and is dropped) → a static class next to it (next to the outermost type for a profile nested in a DTO) with
/// one B ToB(this A source) per map, copying the members with the same name and type. Anything AutoMapper would do
/// beyond that (flattening, constructors) is Manual, except a member whose type pair has its own CreateMap in a profile this fix
/// rewrites (X = source.X?.ToY()), a list or array (copied into a new one, element by element through such a map), see <see cref="MemberValues"/>,
/// and AutoMapper's built-in conversions with a hand-written form that gives the same result (X = source.X.GetValueOrDefault(),
/// source.X.ToString(), Convert.ToInt32(source.X), (Color)source.X, ...), see <see cref="MemberConversions"/>.
/// The rewrite also understands
/// ForMember(d => d.X, o => o.Ignore()) (see <see cref="ForMemberIgnorePattern"/>) and ForMember(d => d.X, o => o.MapFrom(s => expr))
/// (see <see cref="ForMemberMapFromPattern"/>); those two claim the profiles that use them. With the solution, a map that a
/// mapper.Map(source, destination) call uses also gets B MapInto(this A source, B destination) (see <see cref="MapIntoPattern"/>).
/// CreateMap&lt;A, B&gt;().ReverseMap() also gets A ToA(this B source) for the reverse map (unflattening is Manual, TAAK-249).
/// </summary>
public class CreateMapPattern : ISolutionPattern
{
    private const string AutoMapperAssembly = "AutoMapper";

    public virtual string Id => "automapper.create-map";

    public virtual string Description =>
        "Profile with CreateMap<A, B>() without options → static B ToB(this A source) => new() { X = source.X, ... } for the members with the same name and type (or a built-in conversion AutoMapper does the same way); MapInto(this A source, B destination) when the solution calls Map(source, destination); Map with options and ProjectTo are Manual";

    // Map and ProjectTo calls that MapCallPattern does not rewrite are found too: while they run, a removed CreateMap fails at run time,
    // so they make the fix all-or-nothing.
    public virtual IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => n switch
        {
            ClassDeclarationSyntax c => IsProfileWithMaps(c, model) && !UsesForMember(c, ignore: true) && !UsesForMember(c, ignore: false),
            InvocationExpressionSyntax i => model.GetSymbolInfo(i) is var info
                && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "Map" or "ProjectTo" } method
                && method.ContainingAssembly?.Name == AutoMapperAssembly
                && !MapCallPattern.IsMapCall(i, model)
                && !MapIntoPattern.IsMapIntoCall(i, model),
            _ => false,
        }).Select(Anchor).Distinct();

    // Without the solution no MapInto is written: nothing says a Map(source, destination) call needs it.
    public virtual PatternResult Rewrite(SyntaxNode node, SemanticModel model) => Rewrite(node, model, new HashSet<string>(), nested: null);

    public virtual async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(solution);
        return node is TypeDeclarationSyntax
            ? Rewrite(node, model, await MapIntoPattern.TargetsAsync(solution, cancellationToken), await NestedMaps.FromAsync(model, solution, cancellationToken))
            : Rewrite(node, model, new HashSet<string>(), nested: null);
    }

    // The ToB methods of a profile, with nested maps of the whole solution; no MapInto (MapCallPattern only needs the method names).
    internal static async Task<PatternResult> RewriteMethodsAsync(ClassDeclarationSyntax profile, SemanticModel model, Solution solution, CancellationToken cancellationToken) =>
        Rewrite(profile, model, new HashSet<string>(), await NestedMaps.FromAsync(model, solution, cancellationToken));

    // mapInto: the maps (MapIntoPattern.Key) that get a MapInto next to ToB. nested: the maps a member can call (null: this project's).
    private static PatternResult Rewrite(SyntaxNode node, SemanticModel model, IReadOnlySet<string> mapInto, NestedMaps? nested)
    {
        if (node is InvocationExpressionSyntax)
        {
            return PatternResult.Manual(
                "AutoMapper's ProjectTo and Map with options are not migrated yet (automapper.unsupported); while they run, the CreateMap rewrites of this fix stay undone.");
        }

        // node: a profile, or (from Find) the outermost type around a nested one. An extension class must be top-level, so it goes
        // right after that outermost type (the anchor), which then can hold only one profile.
        TypeDeclarationSyntax anchor = node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Last();
        List<ClassDeclarationSyntax> profiles = [.. anchor.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().Where(c => IsProfileWithMaps(c, model))];
        if (profiles is not [{ } profile])
        {
            return PatternResult.Manual($"{anchor.Identifier} holds several profiles ({string.Join(", ", profiles.Select(p => p.Identifier.ValueText))}), whose extension classes would all go next to it; write their mapping methods by hand.");
        }

        INamedTypeSymbol? profileType = model.GetDeclaredSymbol(profile);
        if (profile.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Any(t => t.TypeParameterList is not null))
        {
            return PatternResult.Manual($"Profile {profile.Identifier} is generic or nested in a generic type (its maps can use the type parameters); write its mapping methods by hand.");
        }

        if (profile.Modifiers.Any(SyntaxKind.PartialKeyword))
        {
            return PatternResult.Manual($"Profile {profile.Identifier} is partial (another part can add maps or members they use); write its mapping methods by hand.");
        }

        if (profile.Members.OfType<ConstructorDeclarationSyntax>().ToList() is not [{ } constructor])
        {
            return PatternResult.Manual($"Profile {profile.Identifier} has several constructors (which maps exist depends on the one that runs); write its mapping methods by hand.");
        }

        List<(TypeSyntax Source, TypeSyntax Destination, List<(string Name, string Value)> Members, bool MapInto)> maps = [];
        bool isInternal = false;
        HashSet<string> pairs = new(StringComparer.Ordinal);
        HashSet<string> namespaces = new(StringComparer.Ordinal);
        var values = new MemberValues(nested ?? NestedMaps.From(model), model, anchor.SpanStart, namespaces);
        foreach (SyntaxNode statement in MapStatements(constructor))
        {
            if (!TryParseMap(statement, model, out InvocationExpressionSyntax? invocation, out Dictionary<string, SimpleLambdaExpressionSyntax?>? options,
                    out Dictionary<string, SimpleLambdaExpressionSyntax?>? reverseOptions)
                || invocation.Expression is not GenericNameSyntax { Identifier.ValueText: "CreateMap", TypeArgumentList.Arguments: [{ } source, { } destination] }
                || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly } method)
            {
                return PatternResult.Manual($"'{string.Join(' ', statement.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))}' is not a CreateMap<A, B>() without options; migrate Profile {profile.Identifier} by hand.");
            }

            ITypeSymbol sourceType = method.TypeArguments[0];
            ITypeSymbol destinationType = method.TypeArguments[1];
            if (Problem(sourceType, destinationType, options, model, profileType, values, out List<(string Name, string Value)> members, out string? copied) is string problem)
            {
                return PatternResult.Manual($"CreateMap<{sourceType.Name}, {destinationType.Name}>: {problem}; write the mapping method by hand.");
            }

            bool needsMapInto = mapInto.Contains(MapIntoPattern.Key(sourceType, destinationType));
            if (needsMapInto && copied is not null)
            {
                // Measured (TAAK-245): Map(source, destination) maps into the nested object or list the destination already has.
                return PatternResult.Manual($"CreateMap<{sourceType.Name}, {destinationType.Name}>: Map(source, destination) maps {copied} into the object or collection the destination already holds, MapInto would replace it; write MapInto by hand.");
            }

            Accessibility access = Weakest(Weakest(Effective(sourceType), Effective(destinationType)), Accessibility.Public);
            if (access is not (Accessibility.Public or Accessibility.Internal))
            {
                return PatternResult.Manual($"{sourceType.Name} or {destinationType.Name} is private or protected; an extension method cannot use it.");
            }

            if ((OutsideProblem(options, model, anchor.SpanStart) ?? OutsideProblem(reverseOptions ?? [], model, anchor.SpanStart)) is string outside)
            {
                return PatternResult.Manual($"CreateMap<{sourceType.Name}, {destinationType.Name}>: {outside}; write the mapping method by hand.");
            }

            if (anchor != profile)
            {
                // A nested profile can name a type of its containing types without their names (Command for AddNote.Command).
                (string written, string writtenReverse) = (MethodName(destination), MethodName(source));
                (source, destination) = (AtAnchor(source, sourceType, model, anchor.SpanStart), AtAnchor(destination, destinationType, model, anchor.SpanStart));
                if (MethodName(destination) != written || (reverseOptions is not null && MethodName(source) != writtenReverse))
                {
                    return PatternResult.Manual($"CreateMap<{sourceType.Name}, {destinationType.Name}>: {destinationType.Name} has to be written as {destination} outside {anchor.Identifier}; write the mapping method by hand.");
                }
            }

            isInternal |= access == Accessibility.Internal;
            maps.Add((source, destination, members, needsMapInto));
            if (reverseOptions is not null)
            {
                if (Reverse(sourceType, destinationType, options, reverseOptions, model, profileType, values, mapInto, out List<(string Name, string Value)> reverseMembers,
                    out bool reverseMapInto) is string reverseProblem)
                {
                    return PatternResult.Manual($"ReverseMap of CreateMap<{sourceType.Name}, {destinationType.Name}>: {reverseProblem}; write the mapping method by hand.");
                }

                // After its forward map: MapCallPattern.GeneratedAsync counts the methods the same way.
                maps.Add((destination, source, reverseMembers, reverseMapInto));
            }

            if (!pairs.Add(MapIntoPattern.Key(sourceType, destinationType)) || (reverseOptions is not null && !pairs.Add(MapIntoPattern.Key(destinationType, sourceType))))
            {
                // Two maps for one type pair (a ReverseMap and its own CreateMap<B, A>) would be two methods with the same signature.
                return PatternResult.Manual($"Profile {profile.Identifier} maps {sourceType.Name} and {destinationType.Name} the same way twice (ReverseMap and a CreateMap of its own?); write the mapping methods by hand.");
            }
        }

        string className = ExtensionsName(profile);
        if (model.Compilation.GetSymbolsWithName(className, SymbolFilter.Type).Any())
        {
            return PatternResult.Manual($"A type {className} already exists; write the mapping methods of {profile.Identifier} by hand.");
        }

        return PatternResult.Fixed([
            anchor.ReplaceNode(constructor, Emptied(constructor)),
            Extensions(anchor, className, isInternal ? "internal" : "public", maps)], [.. namespaces.Order(StringComparer.Ordinal)]);
    }

    // The statements of a profile constructor: the block's, or the expression of `=> CreateMap<A, B>()`.
    internal static IReadOnlyList<SyntaxNode> MapStatements(ConstructorDeclarationSyntax constructor) =>
        constructor.Body is { } body ? body.Statements : constructor.ExpressionBody is { } arrow ? [arrow] : [];

    // <Profile>Extensions; a nested profile gets the names of its containing types in front (TodoItemDtoMappingExtensions), because the
    // jason style names every nested profile Mapping.
    internal static string ExtensionsName(ClassDeclarationSyntax profile) =>
        string.Concat(profile.AncestorsAndSelf().OfType<BaseTypeDeclarationSyntax>().Reverse().Select(t => t.Identifier.ValueText)) + "Extensions";

    // The constructor without its maps: an empty block, also for an expression body (which then gets its own lines).
    private static ConstructorDeclarationSyntax Emptied(ConstructorDeclarationSyntax constructor)
    {
        if (constructor.Body is { } body)
        {
            return constructor.WithBody(body.WithStatements([]));
        }

        string indent = constructor.GetLeadingTrivia().LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString();
        string eol = constructor.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } found
            ? found
            : "\n";
        BlockSyntax block = SyntaxFactory.Block()
            .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.EndOfLine(eol), SyntaxFactory.Whitespace(indent)).WithTrailingTrivia(SyntaxFactory.EndOfLine(eol)))
            .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(SyntaxFactory.Whitespace(indent)).WithTrailingTrivia(constructor.SemicolonToken.TrailingTrivia));
        ConstructorDeclarationSyntax headed = constructor.Initializer is { } initializer
            ? constructor.WithInitializer(initializer.WithoutTrailingTrivia())
            : constructor.WithParameterList(constructor.ParameterList.WithoutTrailingTrivia());
        return headed.WithExpressionBody(null).WithSemicolonToken(default).WithBody(block);
    }

    // A type written in a nested profile, as the code next to the outermost type has to write it.
    private static TypeSyntax AtAnchor(TypeSyntax written, ITypeSymbol type, SemanticModel model, int position) =>
        SymbolEqualityComparer.Default.Equals(model.GetSpeculativeTypeInfo(position, written, SpeculativeBindingOption.BindAsTypeOrNamespace).Type, type)
            ? written
            : SyntaxFactory.ParseTypeName(type.ToMinimalDisplayString(model, position));

    // Why a MapFrom expression cannot move to the extension class next to the outermost type: it uses a private or protected member or
    // type (of the profile, a containing type or Profile itself), or a name that means something else there (a member of a containing type).
    private static string? OutsideProblem(Dictionary<string, SimpleLambdaExpressionSyntax?> options, SemanticModel model, int position)
    {
        foreach ((string member, SimpleLambdaExpressionSyntax? lambda) in options)
        {
            foreach (SimpleNameSyntax name in lambda?.Body.DescendantNodesAndSelf().OfType<SimpleNameSyntax>() ?? [])
            {
                if (name.Parent is NameColonSyntax or NameEqualsSyntax || model.GetSymbolInfo(name).Symbol is not { } symbol
                    || symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol)
                {
                    continue;
                }

                if (!model.Compilation.IsSymbolAccessibleWithin(symbol is IMethodSymbol { ReducedFrom: { } extension } ? extension : symbol, model.Compilation.Assembly))
                {
                    return $"MapFrom for {member} uses {name.Identifier.ValueText}, which is private or protected";
                }

                // Only a name that starts a lookup (not the right side of a.b or N.T) is bound again at the new place.
                if (!IsReference(name) || (name.Parent is QualifiedNameSyntax { Right: var right } && right == name)
                    || (name.Parent is AliasQualifiedNameSyntax { Name: var aliased } && aliased == name))
                {
                    continue;
                }

                SymbolInfo there = model.GetSpeculativeSymbolInfo(position, name,
                    SyntaxFacts.IsInNamespaceOrTypeContext(name) ? SpeculativeBindingOption.BindAsTypeOrNamespace : SpeculativeBindingOption.BindAsExpression);
                if (!there.CandidateSymbols.Append(there.Symbol).Any(s => SymbolEqualityComparer.Default.Equals(s?.OriginalDefinition, symbol.OriginalDefinition)))
                {
                    return $"MapFrom for {member} uses {name.Identifier.ValueText}, which means something else outside the type around the profile";
                }
            }
        }

        return null;
    }

    // Why the map cannot be written as plain assignments, or null with the destination members AutoMapper writes (and what to assign),
    // each having a source member of the same name and type (or one MemberValues maps) or a MapFrom expression. copied: the first member
    // that gets a new object or collection. reversed (only for the reverse map of a ReverseMap): destination member → source member of a
    // reversed MapFrom; a destination member without any source is then left alone instead of Manual.
    private static string? Problem(ITypeSymbol source, ITypeSymbol destination, Dictionary<string, SimpleLambdaExpressionSyntax?> options,
        SemanticModel model, INamedTypeSymbol? profileType, MemberValues values, out List<(string Name, string Value)> members, out string? copied,
        Dictionary<string, string>? reversed = null)
    {
        members = [];
        copied = null;
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
        List<ISymbol> destinationMembers = [.. AllMembers(destination).Where(m => m is IPropertySymbol { IsIndexer: false } or IFieldSymbol { IsConst: false })];
        foreach ((string name, SimpleLambdaExpressionSyntax? mapFrom) in options)
        {
            ISymbol? configured = destinationMembers.FirstOrDefault(m => m.Name == name);
            if (configured is null)
            {
                // AutoMapper rejects an unknown member in the configuration; there is nothing sound to leave out.
                return $"ForMember configures {name}, which {destination.Name} does not have as a public property or field";
            }

            if (mapFrom is not null && !(configured is IFieldSymbol { IsReadOnly: false } || configured is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public }))
            {
                return $"ForMember maps {destination.Name}.{name}, which has no public setter";
            }
        }

        foreach (ISymbol target in destinationMembers.Where(m => m is IPropertySymbol or IFieldSymbol { IsReadOnly: false }))
        {
            // Ignore(): AutoMapper leaves the member alone, so the new object does too (also when the source has a member of that name).
            if (options.TryGetValue(target.Name, out SimpleLambdaExpressionSyntax? mapFromLambda))
            {
                if (mapFromLambda is null)
                {
                    continue;
                }

                ITypeSymbol targetType = target is IPropertySymbol property ? property.Type : ((IFieldSymbol)target).Type;
                if (MapFromProblem(target.Name, mapFromLambda, model, profileType, targetType, destination, values, out string value, out bool copies) is string mapFromProblem)
                {
                    return mapFromProblem;
                }

                members.Add((target.Name, value));
                copied ??= copies ? target.Name : null;
                continue;
            }

            bool writable = target is IFieldSymbol || ((IPropertySymbol)target).SetMethod?.DeclaredAccessibility == Accessibility.Public;
            if (reversed is not null && reversed.TryGetValue(target.Name, out string? reversedFrom))
            {
                // Measured (TAAK-249): the reversed MapFrom wins over a source member of the same name.
                if (!writable || readable.FirstOrDefault(m => m.Name == reversedFrom) is not { } reversedMember)
                {
                    return $"{destination.Name}.{target.Name} gets {source.Name}.{reversedFrom} from the reversed MapFrom, but cannot be written or read that way";
                }

                ITypeSymbol reversedType = reversedMember is IPropertySymbol rp ? rp.Type : ((IFieldSymbol)reversedMember).Type;
                ITypeSymbol targetMemberType = target is IPropertySymbol tmp ? tmp.Type : ((IFieldSymbol)target).Type;
                if (values.Problem(target.Name, "source." + reversedFrom, reversedType, targetMemberType, destination, out string? reversedValue, out bool reversedCopy) is string reversedProblem)
                {
                    return reversedProblem;
                }

                if (reversedValue is null)
                {
                    return $"{target.Name} is {targetMemberType.Name} in {destination.Name} but {reversedFrom} is {reversedType.Name} in {source.Name} (AutoMapper converts){MemberValues.NoMap(reversedType, targetMemberType)}";
                }

                members.Add((target.Name, reversedValue));
                copied ??= reversedCopy ? target.Name : null;
                continue;
            }

            List<ISymbol> matches = [.. readable.Where(m => string.Equals(m.Name, target.Name, StringComparison.OrdinalIgnoreCase))];
            if (reversed is not null && matches.Count == 0 && !MayFlatten(target.Name, source))
            {
                // The reverse map has MemberList.None: AutoMapper leaves a member without a source alone (measured, TAAK-249).
                continue;
            }

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
            if (values.Problem(target.Name, "source." + target.Name, from, to, destination, out string? assigned, out bool copy) is string valueProblem)
            {
                return valueProblem;
            }

            if (assigned is null)
            {
                return $"{target.Name} is {from.Name} in {source.Name} but {to.Name} in {destination.Name} (AutoMapper converts){MemberValues.NoMap(from, to)}";
            }

            members.Add((target.Name, assigned));
            copied ??= copy ? target.Name : null;
        }

        return null;
    }

    // Why the reverse map B → A of CreateMap<A, B>(forward).ReverseMap() cannot be written as plain assignments, or null with its members.
    // Measured on AutoMapper 14 (TAAK-249): the reverse map has MemberList.None and maps same-named members like any map; a forward Ignore
    // does not carry over; a forward MapFrom(s => s.X) for Y becomes X = source.Y (a longer path is unflattened, s => s maps into A itself);
    // other MapFrom expressions are not reversed; ReverseMap().ForMember options configure the reverse map and win over a reversed MapFrom;
    // a forward member AutoMapper flattened is unflattened.
    private static string? Reverse(ITypeSymbol source, ITypeSymbol destination, Dictionary<string, SimpleLambdaExpressionSyntax?> forward,
        Dictionary<string, SimpleLambdaExpressionSyntax?> options, SemanticModel model, INamedTypeSymbol? profileType, MemberValues values,
        IReadOnlySet<string> mapInto, out List<(string Name, string Value)> members, out bool needsMapInto)
    {
        members = [];
        needsMapInto = mapInto.Contains(MapIntoPattern.Key(destination, source));
        Dictionary<string, string> reversed = new(StringComparer.Ordinal);
        foreach ((string name, SimpleLambdaExpressionSyntax? mapFrom) in forward)
        {
            if (mapFrom?.Body is not ExpressionSyntax body)
            {
                continue;
            }

            ExpressionSyntax bare = Unwrap(body);
            if (bare is CastExpressionSyntax)
            {
                return $"MapFrom for {name} casts; whether AutoMapper reverses it is not known";
            }

            if (MemberPath(bare, model, mapFrom) is not { } path)
            {
                continue;
            }

            if (path is not [{ } member])
            {
                return path.Count == 0
                    ? $"MapFrom for {name} maps the whole {source.Name}; in reverse AutoMapper maps {name} into the {source.Name} itself"
                    : $"MapFrom for {name} is the path {bare}; in reverse AutoMapper unflattens it (creating the objects on the way)";
            }

            if (!reversed.TryAdd(member, name))
            {
                return $"MapFrom for {reversed[member]} and {name} both read {member}; in reverse AutoMapper writes {member} from one of them";
            }
        }

        // A destination member of the forward map that AutoMapper filled by flattening (B.CustomerName from A.Customer.Name) is unflattened
        // in reverse; the forward rewrite only writes members of the same name, so this only concerns members it leaves alone (not Ignore()d ones).
        List<ISymbol> sourceMembers = [.. AllMembers(source).Where(m => m is IPropertySymbol { IsIndexer: false } or IFieldSymbol)];
        foreach (ISymbol member in AllMembers(destination).Where(m => m is IPropertySymbol { IsIndexer: false } or IFieldSymbol))
        {
            if (forward.TryGetValue(member.Name, out SimpleLambdaExpressionSyntax? configured) && configured is null)
            {
                continue;
            }

            if (!sourceMembers.Any(m => string.Equals(m.Name, member.Name, StringComparison.OrdinalIgnoreCase))
                && sourceMembers.FirstOrDefault(m => member.Name.Length > m.Name.Length && member.Name.StartsWith(m.Name, StringComparison.OrdinalIgnoreCase)) is { } prefix)
            {
                return $"{destination.Name}.{member.Name} may be unflattened into {source.Name}.{prefix.Name} by AutoMapper";
            }
        }

        if (Problem(destination, source, options, model, profileType, values, out members, out string? copied, reversed) is string problem)
        {
            return problem;
        }

        // Measured (TAAK-245, again for the reverse map): Map(source, destination) maps into the nested object or list the destination holds.
        return needsMapInto && copied is not null
            ? $"Map(source, destination) maps {copied} into the object or collection the destination already holds, MapInto would replace it"
            : null;
    }

    // The members of `p.X.Y` (outermost last) when expression is a path of properties and fields from the lambda parameter p, [] for `p` itself, else null.
    private static List<string>? MemberPath(ExpressionSyntax expression, SemanticModel model, SimpleLambdaExpressionSyntax lambda)
    {
        List<string> path = [];
        while (Unwrap(expression) is MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } access
            && model.GetSymbolInfo(access).Symbol is IPropertySymbol or IFieldSymbol { IsStatic: false })
        {
            path.Insert(0, access.Name.Identifier.ValueText);
            expression = access.Expression;
        }

        return Unwrap(expression) is IdentifierNameSyntax name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name).Symbol, model.GetDeclaredSymbol(lambda.Parameter))
            ? path
            : null;
    }

    // Parentheses and the null-forgiving ! are not in the expression tree AutoMapper sees.
    private static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => Unwrap(parenthesized.Expression),
        PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed => Unwrap(suppressed.Operand),
        _ => expression,
    };

    // Whether AutoMapper may fill a destination member that has no source member of its exact name: a member or method of that name in
    // another case or with a Get prefix, or one whose name starts the member's name (flattening: Customer.Name → CustomerName).
    private static bool MayFlatten(string name, ITypeSymbol source) =>
        AllMembers(source).Any(m => (m is IPropertySymbol or IFieldSymbol || m is IMethodSymbol { MethodKind: MethodKind.Ordinary, Parameters: [], ReturnsVoid: false })
            && (string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(m.Name, "Get" + name, StringComparison.OrdinalIgnoreCase)
                || (name.Length > m.Name.Length && name.StartsWith(m.Name, StringComparison.OrdinalIgnoreCase))));

    // Why `s => expr` cannot move into the extension method as `X = expr`, or null with the text of expr (s becomes source).
    // AutoMapper runs a MapFrom expression null-safe (a NullReferenceException gives the default value) and converts types
    // where needed, a plain assignment does neither: so only expressions that cannot throw and need no conversion are moved.
    private static string? MapFromProblem(string member, SimpleLambdaExpressionSyntax lambda, SemanticModel model, INamedTypeSymbol? profileType,
        ITypeSymbol targetType, ITypeSymbol destination, MemberValues values, out string value, out bool copies)
    {
        value = "";
        copies = false;
        if (lambda.Body is not ExpressionSyntax body || model.GetDeclaredSymbol(lambda.Parameter) is not IParameterSymbol parameter || lambda.AsyncKeyword != default)
        {
            return $"MapFrom for {member} is not a lambda with an expression body";
        }

        string parameterName = parameter.Name;
        List<SimpleNameSyntax> parameterUses = [];
        foreach (SyntaxNode node in body.DescendantNodesAndSelf())
        {
            ExpressionSyntax? receiver = null;
            switch (node)
            {
                case ThisExpressionSyntax or BaseExpressionSyntax:
                    return $"MapFrom for {member} uses this or base of the profile";
                case SimpleNameSyntax name when IsReference(name):
                    ISymbol? symbol = model.GetSymbolInfo(name).Symbol;
                    if (SymbolEqualityComparer.Default.Equals(symbol, parameter))
                    {
                        parameterUses.Add(name);
                    }
                    else if (name.Identifier.ValueText == "source" && parameterName != "source" && symbol is not INamedTypeSymbol)
                    {
                        return $"MapFrom for {member} already uses the name source for something else";
                    }
                    else if (!IsAvailableOutsideProfile(symbol, body, profileType))
                    {
                        return $"MapFrom for {member} uses {name.Identifier.ValueText}, which is not available outside the profile";
                    }

                    break;
                case MemberAccessExpressionSyntax access:
                    receiver = access.Expression;
                    break;
                case ElementAccessExpressionSyntax element:
                    receiver = element.Expression;
                    break;
            }

            if (receiver is not null && MayBeNull(receiver, model, parameter))
            {
                return $"MapFrom for {member} dereferences {receiver.ToString().Trim()}, which may be null (AutoMapper returns the default value, a plain assignment throws)";
            }
        }

        ITypeSymbol? from = model.GetTypeInfo(body).Type;
        if (from is null || from.TypeKind == TypeKind.Error)
        {
            return $"MapFrom for {member} has no type of its own (a null literal, for one)";
        }

        ExpressionSyntax replaced = parameterName == "source"
            ? body
            : body.ReplaceNodes(parameterUses, (original, _) => SyntaxFactory.IdentifierName("source").WithTriviaFrom(original));
        value = replaced.WithoutTrivia().ToString();
        if (values.Problem("MapFrom for " + member, value, from, targetType, destination, out string? mapped, out copies) is string valueProblem)
        {
            return valueProblem;
        }

        if (mapped is not null)
        {
            value = mapped;
            return null;
        }

        if (!(model.Compilation.ClassifyConversion(from, targetType) is { IsImplicit: true } conversion && (conversion.IsNumeric || conversion.IsNullable)))
        {
            return $"MapFrom for {member} is {from.Name} but {member} is {targetType.Name} (AutoMapper converts){MemberValues.NoMap(from, targetType)}";
        }

        return null;
    }

    // A name that is read (not the right side of a.b, an initializer member, a named argument or a property name of an anonymous type).
    private static bool IsReference(SimpleNameSyntax name) => name.Parent switch
    {
        MemberAccessExpressionSyntax access => access.Expression == name,
        MemberBindingExpressionSyntax => false,
        NameColonSyntax or NameEqualsSyntax => false,
        AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment => assignment.Left != name,
        _ => true,
    };

    private static bool IsAvailableOutsideProfile(ISymbol? symbol, ExpressionSyntax body, INamedTypeSymbol? profileType) => symbol switch
    {
        null => false,
        INamedTypeSymbol or INamespaceSymbol or ITypeParameterSymbol => true,
        ILocalSymbol or IParameterSymbol or IRangeVariableSymbol =>
            symbol.Locations.Any(l => l.SourceTree == body.SyntaxTree && body.Span.Contains(l.SourceSpan)),
        _ => symbol.IsStatic && !SymbolEqualityComparer.Default.Equals(symbol.ContainingType, profileType),
    };

    // Whether `receiver.Something` can throw: a reference type or Nullable<T> that is not the lambda parameter and not known to be non-null.
    private static bool MayBeNull(ExpressionSyntax receiver, SemanticModel model, IParameterSymbol parameter)
    {
        ISymbol? symbol = model.GetSymbolInfo(receiver).Symbol;
        if (SymbolEqualityComparer.Default.Equals(symbol, parameter) || symbol is INamedTypeSymbol or INamespaceSymbol)
        {
            return false;
        }

        TypeInfo info = model.GetTypeInfo(receiver);
        if (info.Type is not { } type || (!type.IsReferenceType && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T))
        {
            return false;
        }

        return info.Nullability.FlowState != NullableFlowState.NotNull;
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

    // Find reports a nested profile at its outermost type: the rewrite replaces that type, so the extension class can go next to it.
    protected static SyntaxNode Anchor(SyntaxNode node) => node is ClassDeclarationSyntax c ? c.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Last() : node;

    protected internal static bool IsProfileWithMaps(ClassDeclarationSyntax c, SemanticModel model) =>
        model.GetDeclaredSymbol(c) is INamedTypeSymbol type && IsProfile(type)
        && c.Members.OfType<ConstructorDeclarationSyntax>().Any(ctor => ctor.Body is { Statements.Count: > 0 } || ctor.ExpressionBody is not null);

    // Syntactic: whether a ForMember(..., o => o.Ignore()) (ignore) or ForMember(..., o => o.MapFrom(s => expr)) sits in the profile,
    // so exactly one pattern claims it: MapFrom before Ignore before plain CreateMap.
    protected static bool UsesForMember(ClassDeclarationSyntax profile, bool ignore) =>
        profile.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i =>
            i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ForMember" }
            && (ignore ? ForMemberOption(i) is { MapFrom: null } : IsMapFromCall(i)));

    // Any `o => o.MapFrom(...)`, also the forms that stay Manual (resolver lambda with two parameters, string name, generic): the MapFrom pattern reports them.
    private static bool IsMapFromCall(InvocationExpressionSyntax forMember) =>
        forMember.ArgumentList.Arguments is [_, { Expression: SimpleLambdaExpressionSyntax { Body: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "MapFrom" } } } }];

    // `CreateMap<A, B>()` (or with a MemberList) or `CreateMap<A, B>().ForMember(d => d.X, o => o.Ignore())...`, optionally followed by
    // `.ReverseMap()` and ForMember options of the reverse map (reverse: those options, null without ReverseMap); anything else in the chain is no match.
    // statement: a statement of a block body, or the arrow clause of an expression body.
    private static bool TryParseMap(SyntaxNode statement, SemanticModel model, [NotNullWhen(true)] out InvocationExpressionSyntax? createMap,
        [NotNullWhen(true)] out Dictionary<string, SimpleLambdaExpressionSyntax?>? options, out Dictionary<string, SimpleLambdaExpressionSyntax?>? reverse)
    {
        createMap = null;
        options = [];
        reverse = null;
        if (((statement as ExpressionStatementSyntax)?.Expression ?? (statement as ArrowExpressionClauseSyntax)?.Expression) is not InvocationExpressionSyntax expression)
        {
            return false;
        }

        // The chain is read from the outside in: options seen before ReverseMap (outermost first) configure the reverse map.
        if (ReverseMapCall(expression, model) is { } reverseMap)
        {
            if (!TryParseOptions(ref expression, model, options) || expression != reverseMap)
            {
                return false;
            }

            reverse = options;
            options = [];
            expression = (InvocationExpressionSyntax)((MemberAccessExpressionSyntax)reverseMap.Expression).Expression;
        }

        if (!TryParseOptions(ref expression, model, options))
        {
            return false;
        }

        createMap = expression;
        return expression.ArgumentList.Arguments is [] || (expression.ArgumentList.Arguments is [{ } memberList] && IsMemberList(memberList, model));
    }

    // The `.ReverseMap()` call of AutoMapper in the chain of expression, or null.
    private static InvocationExpressionSyntax? ReverseMapCall(InvocationExpressionSyntax expression, SemanticModel model)
    {
        for (InvocationExpressionSyntax? call = expression; call is { Expression: MemberAccessExpressionSyntax access }; call = access.Expression as InvocationExpressionSyntax)
        {
            if (access is { Name.Identifier.ValueText: "ReverseMap", Expression: InvocationExpressionSyntax } && call.ArgumentList.Arguments.Count == 0
                && model.GetSymbolInfo(call).Symbol is IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly })
            {
                return call;
            }
        }

        return null;
    }

    // Syntactic: whether the CreateMap call has `.ReverseMap()` in its chain (the profile rewrite then also writes A ToA(this B source)).
    internal static bool HasReverseMap(InvocationExpressionSyntax createMap)
    {
        for (SyntaxNode node = createMap; node.Parent is MemberAccessExpressionSyntax access && access.Expression == node && access.Parent is InvocationExpressionSyntax call; node = call)
        {
            if (access.Name.Identifier.ValueText == "ReverseMap" && call.ArgumentList.Arguments.Count == 0)
            {
                return true;
            }
        }

        return false;
    }

    // Strips `.ForMember(...)` calls off expression into options, until the call they hang on.
    private static bool TryParseOptions(ref InvocationExpressionSyntax expression, SemanticModel model, Dictionary<string, SimpleLambdaExpressionSyntax?> options)
    {
        while (expression is { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ForMember" } access } forMember)
        {
            if (ForMemberOption(forMember) is not var (member, mapFrom)
                || model.GetSymbolInfo(forMember).Symbol is not IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly }
                || (mapFrom is not null && model.GetSymbolInfo((InvocationExpressionSyntax)mapFrom.Parent!.Parent!.Parent!).Symbol is not IMethodSymbol { Name: "MapFrom", ContainingAssembly.Name: AutoMapperAssembly })
                || access.Expression is not InvocationExpressionSyntax inner
                || !options.TryAdd(member, mapFrom))
            {
                return false;
            }

            expression = inner;
        }

        return true;
    }

    // CreateMap<A, B>(MemberList.X) or (memberList: MemberList.X): AutoMapper 14 reads the member list only in AssertConfigurationIsValid
    // (measured, TAAK-246), so the map is the same as without it. Only a constant: dropping any other expression could drop a side effect.
    private static bool IsMemberList(ArgumentSyntax argument, SemanticModel model) =>
        argument is { RefKindKeyword.RawKind: 0, NameColon: null or { Name.Identifier.ValueText: "memberList" } }
        && model.GetTypeInfo(argument.Expression).Type is INamedTypeSymbol { TypeKind: TypeKind.Enum, Name: "MemberList", ContainingAssembly.Name: AutoMapperAssembly }
        && model.GetConstantValue(argument.Expression).HasValue;

    // The X of `.ForMember(d => d.X, o => o.Ignore())` (MapFrom null) or `.ForMember(d => d.X, o => o.MapFrom(s => expr))` (MapFrom the lambda),
    // null for any other form (string member name, other option, nested member, block lambda, generic MapFrom).
    private static (string Member, SimpleLambdaExpressionSyntax? MapFrom)? ForMemberOption(InvocationExpressionSyntax forMember)
    {
        if (forMember.ArgumentList.Arguments is not [
            { Expression: SimpleLambdaExpressionSyntax { Parameter.Identifier.ValueText: { } d, Body: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target, Name: { } name } } },
            { Expression: SimpleLambdaExpressionSyntax { Parameter.Identifier.ValueText: { } o, Body: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax options } access } call } }]
            || target.Identifier.ValueText != d || options.Identifier.ValueText != o)
        {
            return null;
        }

        return (access.Name, call.ArgumentList.Arguments) switch
        {
            (IdentifierNameSyntax { Identifier.ValueText: "Ignore" }, []) => (name.Identifier.ValueText, null),
            (IdentifierNameSyntax { Identifier.ValueText: "MapFrom" }, [{ Expression: SimpleLambdaExpressionSyntax { Body: ExpressionSyntax } lambda }]) => (name.Identifier.ValueText, lambda),
            _ => null,
        };
    }

    internal static bool IsProfile(INamedTypeSymbol type)
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
    internal static Accessibility Effective(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => Effective(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Aggregate(
            named.ContainingType is { } outer ? Weakest(named.DeclaredAccessibility, Effective(outer)) : named.DeclaredAccessibility,
            (access, argument) => Weakest(access, Effective(argument))),
        _ => Accessibility.Public,
    };

    internal static Accessibility Weakest(Accessibility a, Accessibility b) => (a, b) switch
    {
        (Accessibility.Public, _) => b,
        (_, Accessibility.Public) => a,
        (Accessibility.Internal, Accessibility.Internal) => Accessibility.Internal,
        _ => Accessibility.Private,
    };

    // ToB for CreateMap<A, B>: the last name of the destination type as written.
    internal static string MethodName(TypeSyntax destination) =>
        "To" + (destination is GenericNameSyntax generic ? generic.Identifier.ValueText : destination.ToString().Split('.')[^1]);

    // The static class goes right after the profile (the outermost type around a nested one), with its indentation and line endings.
    private static MemberDeclarationSyntax Extensions(TypeDeclarationSyntax anchor, string className, string access,
        List<(TypeSyntax Source, TypeSyntax Destination, List<(string Name, string Value)> Members, bool MapInto)> maps)
    {
        string indent = anchor.GetLeadingTrivia().LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString();
        string eol = anchor.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } found
            ? found
            : "\n";
        var text = new StringBuilder();
        text.Append(access).Append(" static class ").Append(className).Append(eol).Append(indent).Append('{').Append(eol);
        foreach ((TypeSyntax source, TypeSyntax destination, List<(string Name, string Value)> members, _) in maps)
        {
            if (text[^(eol.Length + 1)] != '{')
            {
                text.Append(eol);
            }

            text.Append(indent).Append("    ").Append(access).Append(" static ").Append(destination).Append(' ').Append(MethodName(destination))
                .Append("(this ").Append(source).Append(" source) => new()");
            if (members.Count == 0)
            {
                text.Append(';').Append(eol);
                continue;
            }

            text.Append(eol).Append(indent).Append("    {").Append(eol);
            foreach ((string member, string value) in members)
            {
                text.Append(indent).Append("        ").Append(member).Append(" = ").Append(value).Append(',').Append(eol);
            }

            text.Append(indent).Append("    };").Append(eol);
        }

        // After all ToB methods: MapCallPattern pairs the i-th method with the i-th CreateMap.
        foreach ((TypeSyntax source, TypeSyntax destination, List<(string Name, string Value)> members, _) in maps.Where(m => m.MapInto))
        {
            text.Append(eol).Append(indent).Append("    ").Append(access).Append(" static ").Append(destination)
                .Append(" MapInto(this ").Append(source).Append(" source, ").Append(destination).Append(" destination)").Append(eol)
                .Append(indent).Append("    {").Append(eol);
            foreach ((string member, string value) in members)
            {
                text.Append(indent).Append("        destination.").Append(member).Append(" = ").Append(value).Append(';').Append(eol);
            }

            text.Append(indent).Append("        return destination;").Append(eol).Append(indent).Append("    }").Append(eol);
        }

        text.Append(indent).Append('}').Append(eol);
        // The blank line before it is set as trivia: the parser would drop a leading line ending.
        return SyntaxFactory.ParseMemberDeclaration(text.ToString())!.WithLeadingTrivia(SyntaxFactory.EndOfLine(eol), SyntaxFactory.Whitespace(indent));
    }
}
