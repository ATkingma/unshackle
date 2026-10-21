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
/// beyond that (ConstructUsing) is Manual, except a member AutoMapper reads under another name (any case, GetX(), flattening:
/// CustomerName = source.Customer?.Name, see <see cref="Flattening"/>), a destination with one public constructor (new(source.X, ...), parameters matched by name in any case), a member whose type pair has its own CreateMap in a profile this fix
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
    internal const string AutoMapperAssembly = "AutoMapper";

    public virtual string Id => "automapper.create-map";

    public virtual string Description =>
        "Profile with CreateMap<A, B>() without options → static B ToB(this A source) => new() { X = source.X, ... } for the members with the same name and type (or a built-in conversion AutoMapper does the same way); MapInto(this A source, B destination) when the solution calls Map(source, destination); AToBProjection when the solution calls ProjectTo; Map with options is Manual";

    // Map and ProjectTo calls that MapCallPattern does not rewrite are found too: while they run, their CreateMap has to stay (TAAK-272).
    public virtual IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => n switch
        {
            ClassDeclarationSyntax c => ProfileSyntax.IsProfileWithMaps(c, model) && !ProfileSyntax.UsesForMember(c, ignore: true) && !ProfileSyntax.UsesForMember(c, ignore: false),
            InvocationExpressionSyntax i => IsOtherMapCall(i, model),
            _ => false,
        }).Select(ProfileSyntax.Anchor).Distinct();

    // Without the solution no MapInto is written: nothing says a Map(source, destination) call needs it.
    public virtual PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Rewrite(node, model, new HashSet<string>(), nested: null, projectTo: new HashSet<string>(), asserted: Asserts(model), MapOwners.None, problems: null);

    public virtual async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        if (node is not TypeDeclarationSyntax)
        {
            return await MapOwners.KeptCallAsync(node, model, solution, cancellationToken)
                ?? Rewrite(node, model, new HashSet<string>(), nested: null, projectTo: new HashSet<string>(), asserted: true, MapOwners.None, problems: null);
        }

        return Rewrite(node, model, await MapIntoPattern.TargetsAsync(solution, cancellationToken), await NestedMaps.FromAsync(model, solution, cancellationToken),
            await ProjectToPattern.TargetsAsync(solution, cancellationToken), await AssertsAsync(solution, cancellationToken), await MapOwners.ForAsync(solution, model, cancellationToken), problems: null);
    }

    // A Map or ProjectTo of AutoMapper that none of the call patterns rewrites (Map with options, the untyped overloads).
    internal static bool IsOtherMapCall(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation) is var info
        && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "Map" or "ProjectTo" } method
        && method.ContainingAssembly?.Name == AutoMapperAssembly
        && !MapCallPattern.IsMapCall(invocation, model)
        && !MapIntoPattern.IsMapIntoCall(invocation, model)
        && !ProjectToPattern.IsProjectToCall(invocation, model);

    // The ToB methods of a profile, with nested maps of the whole solution; no MapInto (MapCallPattern only needs the method names).
    // While the plan is built, one rewrite per profile and plan size is enough.
    internal static async Task<PatternResult> RewriteMethodsAsync(ClassDeclarationSyntax profile, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        MapOwners owners = await MapOwners.ForAsync(solution, model, cancellationToken);
        if (owners.Rewrites.TryGetValue(profile, out PatternResult? done))
        {
            return done;
        }

        PatternResult result = Rewrite(profile, model, new HashSet<string>(), await NestedMaps.FromAsync(model, solution, cancellationToken), projectTo: new HashSet<string>(),
            await AssertsAsync(solution, cancellationToken), owners, problems: null);
        if (owners != MapOwners.None)
        {
            owners.Rewrites[profile] = result;
        }

        return result;
    }

    // Like RewriteMethodsAsync, with the projection of the map with this key (MapIntoPattern.Key) written too: the parameters of ProjectTo's call come from it.
    internal static async Task<PatternResult> RewriteProjectionAsync(ClassDeclarationSyntax profile, SemanticModel model, Solution solution, string key, CancellationToken cancellationToken) =>
        Rewrite(profile, model, new HashSet<string>(), await NestedMaps.FromAsync(model, solution, cancellationToken), projectTo: new HashSet<string> { key },
            await AssertsAsync(solution, cancellationToken), await MapOwners.ForAsync(solution, model, cancellationToken), problems: null);

    // For the plan (MapOwners): the statements of the profile that cannot be rewritten with the plan so far, or one row without a statement
    // when the whole profile cannot be.
    internal static async Task<List<(SyntaxNode? Statement, string Reason)>> PlanAsync(ClassDeclarationSyntax profile, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        List<(SyntaxNode? Statement, string Reason)> problems = [];
        PatternResult result = Rewrite(profile, model, await MapIntoPattern.TargetsAsync(solution, cancellationToken), await NestedMaps.FromAsync(model, solution, cancellationToken),
            await ProjectToPattern.TargetsAsync(solution, cancellationToken), await AssertsAsync(solution, cancellationToken), await MapOwners.ForAsync(solution, model, cancellationToken), problems);
        if (result is ManualResult { Kept: KeptReason.None, Reason: var reason })
        {
            problems.Add((null, $"Profile {profile.Identifier.ValueText}: {reason}"));
        }

        return problems;
    }

    // Whether the solution (or, without it, the compilation) calls AssertConfigurationIsValid anywhere. Such a check rejects a map with a
    // destination member AutoMapper finds no source for, so with it a member that looks unmatched is taken as mapped in a way the fix
    // does not see (TAAK-261); counting any call, not only one that reaches the map, keeps that safe.
    private static bool Asserts(SemanticModel model) =>
        model.Compilation.SyntaxTrees.Any(t => t.GetText().ToString().Contains("AssertConfigurationIsValid", StringComparison.Ordinal));

    private static async Task<bool> AssertsAsync(Solution solution, CancellationToken cancellationToken)
    {
        foreach (Document document in solution.Projects.SelectMany(p => p.Documents))
        {
            if ((await document.GetTextAsync(cancellationToken)).ToString().Contains("AssertConfigurationIsValid", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    // mapInto: the maps (MapIntoPattern.Key) that get a MapInto next to ToB. nested: the maps a member can call (null: this project's).
    // projectTo: the maps that a ProjectTo call uses, which get an Expression<Func<A, B>> AToBProjection next to ToB (see Projections).
    // asserted: the solution calls AssertConfigurationIsValid (see Asserts).
    // owners: the plan of the partial migration (TAAK-272), whose maps stay in the constructor. problems: null to stop at the first statement that
    // cannot be rewritten (Manual for the profile); else that statement is added to it and left out, as the plan needs.
    private static PatternResult Rewrite(SyntaxNode node, SemanticModel model, IReadOnlySet<string> mapInto, NestedMaps? nested, IReadOnlySet<string> projectTo, bool asserted,
        MapOwners owners, List<(SyntaxNode? Statement, string Reason)>? problems)
    {
        if (node is InvocationExpressionSyntax)
        {
            return PatternResult.Manual(
                "AutoMapper's ProjectTo and Map with options are not migrated yet (automapper.unsupported); while they run, the CreateMap rewrites of this fix stay undone.");
        }

        // node: a profile, or (from Find) the outermost type around a nested one. An extension class must be top-level, so it goes
        // right after that outermost type (the anchor), which then can hold only one profile.
        TypeDeclarationSyntax anchor = node.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Last();
        List<ClassDeclarationSyntax> profiles = [.. anchor.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().Where(c => ProfileSyntax.IsProfileWithMaps(c, model))];
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

        // A Profile keeps its maps in the constructor; any other class in the lambda of its one `new MapperConfiguration(cfg => ...)` (TAAK-286).
        ConstructorDeclarationSyntax? constructor = null;
        LambdaExpressionSyntax? lambda = null;
        if (profileType is not null && ProfileSyntax.IsProfile(profileType))
        {
            if (profile.Members.OfType<ConstructorDeclarationSyntax>().ToList() is not [{ } only])
            {
                return PatternResult.Manual(SeveralConstructorsReason(profile, model));
            }

            constructor = only;
        }
        else if (ProfileSyntax.ConfigurationLambdas(profile, model) is not [{ } single])
        {
            return PatternResult.Manual($"{profile.Identifier} builds several MapperConfiguration objects with CreateMap statements (which maps exist depends on the one that is used); write their mapping methods by hand.");
        }
        else
        {
            lambda = single;
        }

        IParameterSymbol? configuration = lambda is null ? null : ProfileSyntax.LambdaParameter(lambda, model);
        string owner = lambda is null ? $"Profile {profile.Identifier}" : $"the MapperConfiguration in {profile.Identifier}";

        List<(TypeSyntax Source, TypeSyntax Destination, List<(string Name, string Value)> Members, bool MapInto, string Creation)> maps = [];
        bool isInternal = false;
        HashSet<string> pairs = new(StringComparer.Ordinal);
        HashSet<string> namespaces = new(StringComparer.Ordinal);
        NestedMaps allMaps = nested ?? NestedMaps.From(model);
        var values = new MemberValues(allMaps, model, anchor.SpanStart, namespaces);
        Dictionary<string, (ITypeSymbol Source, ITypeSymbol Destination, IReadOnlyList<ProjectionMember> Members)> projectable = new(StringComparer.Ordinal);
        List<(TypeSyntax Source, TypeSyntax Destination, string Key, string Name, SyntaxNode Statement)> projected = [];
        List<CapturedLocal> captured = [];
        List<SyntaxNode> kept = [];
        string? keptReason = null;
        foreach (SyntaxNode statement in lambda is null ? ProfileSyntax.MapStatements(constructor!) : ProfileSyntax.LambdaStatements(lambda))
        {
            if (CapturedLocal.TryParse(statement, model) is { } local)
            {
                // `int userId = 0;` in the constructor: a MapFrom lambda reads it, ProjectTo fills it per call (TAAK-265).
                captured.Add(local);
                values.Captured.Add(local.Symbol);
                continue;
            }

            if (MapOwners.StatementPair(statement, model) is var (keptFrom, keptTo, _) && owners.Reason(keptFrom, keptTo) is string why)
            {
                // The plan keeps this map on AutoMapper (TAAK-272): the statement stays in the trimmed constructor.
                kept.Add(statement);
                keptReason ??= $"CreateMap<{keptFrom.Name}, {keptTo.Name}>: {why}";
                continue;
            }

            HashSet<string> namespacesBefore = [.. namespaces];
            if (Statement(statement) is string problem)
            {
                // Without a plan, or for a statement that is no CreateMap (a setting acts on every map), the whole profile stays.
                if (problems is null || MapOwners.StatementPair(statement, model) is null)
                {
                    return PatternResult.Manual(problem);
                }

                problems.Add((statement, problem));
                namespaces.Clear();
                namespaces.UnionWith(namespacesBefore);
            }
            else if (MapOwners.StatementPair(statement, model) is var (sharedFrom, sharedTo, reverse)
                && (owners.Shared(sharedFrom, sharedTo) ?? (reverse ? owners.Shared(sharedTo, sharedFrom) : null)) is not null)
            {
                // A class under test keeps calling IMapper for this map (TAAK-279): the statement stays next to its generated methods.
                kept.Add(statement);
            }
        }

        if (maps.Count == 0 && keptReason is not null)
        {
            return PatternResult.KeptManual(KeptReason.ProfileKept, keptReason);
        }


        // One CreateMap statement: its methods are added to maps, or the reason it cannot be rewritten.
        string? Statement(SyntaxNode statement)
        {
            if (!ProfileSyntax.TryParseMap(statement, model, out InvocationExpressionSyntax? invocation, out Dictionary<string, SimpleLambdaExpressionSyntax?>? options,
                    out Dictionary<string, SimpleLambdaExpressionSyntax?>? reverseOptions, out Dictionary<string, InvocationExpressionSyntax> resolverCalls,
                    out InvocationExpressionSyntax? convertUsing, out Dictionary<string, InvocationExpressionSyntax> converters, out MemberOptions extra, out MemberOptions reverseExtra)
                || ProfileSyntax.CreateMapName(invocation, configuration, model) is not GenericNameSyntax { Identifier.ValueText: "CreateMap", TypeArgumentList.Arguments: [{ } source, { } destination] }
                || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly } method)
            {
                string written = string.Join(' ', statement.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                return extra.Unsupported is { } unsupported
                    ? $"{unsupported}; migrate {owner} by hand."
                    : UnsupportedOptions.Reason(statement, model) is string advice
                        ? $"'{written}' in {owner}: {advice} (automapper.unsupported)."
                        : $"'{written}' is not a CreateMap<A, B>() without options; migrate {owner} by hand.";
            }

            ITypeSymbol sourceType = method.TypeArguments[0];
            ITypeSymbol destinationType = method.TypeArguments[1];
            if (reverseExtra.ConstructUsing is not null)
            {
                // The reverse map's own construction (ReverseMap().ConstructUsing(...)) is not measured with the reverse rewrite.
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: ConstructUsing after ReverseMap builds the reverse destination; write the mapping methods by hand.";
            }

            if ((convertUsing is not null || converters.Count > 0) && projectTo.Contains(MapIntoPattern.Key(sourceType, destinationType)))
            {
                // A converter runs code per object; an expression the query provider translates cannot call it (TAAK-253).
                return $"ProjectTo<{destinationType.Name}> uses CreateMap<{sourceType.Name}, {destinationType.Name}>, which has a ConvertUsing converter; write the projection by hand.";
            }

            if ((!extra.IsEmpty || extra.HasForPath) && projectTo.Contains(MapIntoPattern.Key(sourceType, destinationType)))
            {
                // Not measured for a projection (TAAK-255): the conditional and nested-object forms stay out of ProjectTo.
                return $"ProjectTo<{destinationType.Name}> uses CreateMap<{sourceType.Name}, {destinationType.Name}>, which has a Condition or ForPath; write the projection by hand.";
            }

            if (reverseOptions is not null && (extra.CtorParams.Count > 0 || reverseExtra.CtorParams.Count > 0))
            {
                // Not measured (TAAK-270): whether ReverseMap carries a ForCtorParam over.
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: ForCtorParam next to ReverseMap is not migrated; write the mapping methods by hand.";
            }

            if (reverseOptions is not null && resolverCalls.Count > 0)
            {
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: ReverseMap next to a value resolver or a MapFrom lambda with more parameters (AutoMapper does not reverse those); write the mapping methods by hand.";
            }

            if (resolverCalls.Count > 0 && projectTo.Contains(MapIntoPattern.Key(sourceType, destinationType)))
            {
                // A resolver runs code per object; an expression the query provider translates cannot call it.
                return $"ProjectTo<{destinationType.Name}> uses CreateMap<{sourceType.Name}, {destinationType.Name}>, which has a value resolver or a MapFrom lambda with more parameters; write the projection by hand.";
            }

            List<ProjectionMember> read = [];
            List<(string Name, string Value)> members = [];
            string? copied = null;
            string creation;
            if (convertUsing is not null)
            {
                // ConvertUsing replaces the whole map: ToB returns what the converter returns (TAAK-253).
                if (Converters.TypeProblem(convertUsing, sourceType, destinationType, MapMethodWriter.MethodName(destination), model, profileType, out creation) is string converterProblem)
                {
                    return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: {converterProblem}; write the mapping method by hand.";
                }
            }
            else if (CreateMapProblems.Problem(sourceType, destinationType, options, model, profileType, values, out members, out copied, out creation,
                resolvers: new ValueResolvers(resolverCalls, model, anchor.SpanStart, profileType), read: read, converters: converters, extra: extra, asserted: asserted) is string problem)
            {
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: {problem}; write the mapping method by hand.";
            }

            // MaxDepth and PreserveReferences only act on nested objects and collections (measured, TAAK-257).
            // Measured (TAAK-266): MaxDepth(n) with n of 2 or more also changes nothing on nested copies as long as no type pair can stack in
            // itself, and a cycle is already Manual in MemberValues; MaxDepth(1) cuts a collection member off, and a ProjectTo expression is
            // built with the depth, so those stay Manual.
            if (UnsupportedOptions.DepthOption(statement, model) is string depth
                && (reverseOptions is not null || (copied is not null
                    && !(UnsupportedOptions.MaxDepthLimit(statement, model) is >= 2 && !projectTo.Contains(MapIntoPattern.Key(sourceType, destinationType))))))
            {
                string why = depth == "MaxDepth" && copied is not null && reverseOptions is null
                    ? "; MaxDepth is only dropped from 2 up and without ProjectTo"
                    : "";
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: {depth} acts on {copied ?? "the reverse map"}, which the hand-written method copies without it{why}; write the mapping method by hand (automapper.unsupported).";
            }

            bool needsMapInto = mapInto.Contains(MapIntoPattern.Key(sourceType, destinationType));
            if (CapturedLocal.Used(captured, members) is { Count: > 0 } usedLocals && (needsMapInto || reverseOptions is not null))
            {
                // Not measured (TAAK-265): MapInto and the reverse map do not take the captured variable as a parameter.
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: a MapFrom reads {string.Join(", ", usedLocals.Select(c => c.Name))}, a variable of the profile constructor that ProjectTo fills per call; this is not migrated together with Map(source, destination) or ReverseMap; write the mapping methods by hand.";
            }

            if ((!extra.IsEmpty || extra.HasForPath) && needsMapInto)
            {
                // Not migrated (TAAK-255): Map(source, destination) keeps the existing value or object.
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: a Condition or ForPath is not migrated together with Map(source, destination) (MapInto); write the mapping methods by hand.";
            }

            if (extra.HasForPath && reverseOptions is not null)
            {
                // Measured (TAAK-264): ReverseMap reverses a forward ForPath (ForPath(d => d.A.B, MapFrom(s => s.T)) gives T = source.A.B in reverse); a Condition it does not carry over.
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: ReverseMap reverses the ForPath of the forward map (it reads the path back into the source member); write the mapping methods by hand.";
            }
            if (needsMapInto && convertUsing is not null)
            {
                // Measured (TAAK-253): Map(source, destination) returns the converter's result and leaves the destination alone.
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: Map(source, destination) runs ConvertUsing, which returns a new value instead of filling the destination; migrate that call by hand.";
            }

            if (needsMapInto && copied is not null)
            {
                // Measured (TAAK-245): Map(source, destination) maps into the nested object or list the destination already has.
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: Map(source, destination) maps {copied} into the object or collection the destination already holds, MapInto would replace it; write MapInto by hand.";
            }

            if (needsMapInto && creation != "new()")
            {
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: Map(source, destination) fills an existing {destinationType.Name}, whose constructor parameters (or ConstructUsing) MapInto cannot run; write MapInto by hand.";
            }

            Accessibility access = MapAccessibility.Weakest(MapAccessibility.Weakest(MapAccessibility.Effective(sourceType), MapAccessibility.Effective(destinationType)), Accessibility.Public);
            if (access is not (Accessibility.Public or Accessibility.Internal))
            {
                return $"{sourceType.Name} or {destinationType.Name} is private or protected; an extension method cannot use it.";
            }

            if ((CreateMapProblems.OutsideProblem(options, model, anchor.SpanStart) ?? CreateMapProblems.OutsideProblem(reverseOptions ?? [], model, anchor.SpanStart) ?? CreateMapProblems.OutsideProblem(extra.Lambdas(), model, anchor.SpanStart) ?? CreateMapProblems.OutsideProblem(reverseExtra.Lambdas(), model, anchor.SpanStart)
                ?? (extra.ConstructUsing is { ArgumentList.Arguments: [{ Expression: LambdaExpressionSyntax { Body: { } constructed } }] } ? CreateMapProblems.OutsideProblem("ConstructUsing", constructed, model, anchor.SpanStart) : null)) is string outside)
            {
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: {outside}; write the mapping method by hand.";
            }

            if (anchor != profile)
            {
                // A nested profile can name a type of its containing types without their names (Command for AddNote.Command).
                (string written, string writtenReverse) = (MapMethodWriter.MethodName(destination), MapMethodWriter.MethodName(source));
                (source, destination) = (MapMethodWriter.AtAnchor(source, sourceType, model, anchor.SpanStart), MapMethodWriter.AtAnchor(destination, destinationType, model, anchor.SpanStart));
                if (MapMethodWriter.MethodName(destination) != written || (reverseOptions is not null && MapMethodWriter.MethodName(source) != writtenReverse))
                {
                    return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: {destinationType.Name} has to be written as {destination} outside {anchor.Identifier}; write the mapping method by hand.";
                }
            }

            if (reverseOptions is not null && projectTo.Contains(MapIntoPattern.Key(destinationType, sourceType)))
            {
                return $"ProjectTo<{sourceType.Name}> uses the ReverseMap of CreateMap<{sourceType.Name}, {destinationType.Name}>, which this fix writes no projection for; write the projection by hand.";
            }
            List<(string Name, string Value)> reverseMembers = [];
            bool reverseMapInto = false;
            string reverseCreation = "";
            if (reverseOptions is not null
                && CreateMapProblems.Reverse(sourceType, destinationType, options, reverseOptions, reverseExtra, model, profileType, values, mapInto, out reverseMembers,
                    out reverseMapInto, out reverseCreation) is string reverseProblem)
            {
                return $"ReverseMap of CreateMap<{sourceType.Name}, {destinationType.Name}>: {reverseProblem}; write the mapping method by hand.";
            }

            string key = MapIntoPattern.Key(sourceType, destinationType);
            string reverseKey = MapIntoPattern.Key(destinationType, sourceType);
            if (pairs.Contains(key) || (reverseOptions is not null && (pairs.Contains(reverseKey) || key == reverseKey)))
            {
                // Two maps for one type pair (a ReverseMap and its own CreateMap<B, A>) would be two methods with the same signature.
                return $"{char.ToUpperInvariant(owner[0])}{owner[1..]} maps {sourceType.Name} and {destinationType.Name} the same way twice (ReverseMap and a CreateMap of its own?); write the mapping methods by hand.";
            }

            // Only a statement without a problem adds its methods: with a plan (TAAK-272) one with a problem stays in the profile.
            pairs.Add(key);
            if (creation == "new()" && converters.Count == 0)
            {
                // A projection needs the members as an object initializer; a constructor call is not measured for ProjectTo (left out).
                projectable.TryAdd(key, (sourceType, destinationType, read));
            }

            isInternal |= access == Accessibility.Internal;
            maps.Add((source, destination, members, needsMapInto, creation));
            if (projectTo.Contains(key))
            {
                projected.Add((source, destination, key, Projections.Name(sourceType, destinationType), statement));
            }

            if (reverseOptions is not null)
            {
                // After its forward map: MapCallPattern.GeneratedAsync counts the methods the same way.
                pairs.Add(reverseKey);
                maps.Add((destination, source, reverseMembers, reverseMapInto, reverseCreation));
            }

            return null;
        }

        string className = MapMethodWriter.ExtensionsName(profile);
        if (model.Compilation.GetSymbolsWithName(className, SymbolFilter.Type).Any())
        {
            return PatternResult.Manual($"A type {className} already exists; write the mapping methods of {profile.Identifier} by hand.");
        }

        // After all maps are known: a nested object or list of a later CreateMap is inlined too.
        var projections = new Projections(projectable, allMaps, model, anchor.SpanStart);
        List<(TypeSyntax Source, TypeSyntax Destination, string Name, List<(string Name, string Value)> Members, List<CapturedLocal> Parameters)> withProjection = [];
        foreach ((TypeSyntax source, TypeSyntax destination, string key, string name, SyntaxNode statement) in projected)
        {
            if (withProjection.Any(w => w.Name == name))
            {
                return PatternResult.Manual($"Two maps of {profile.Identifier} would both get a projection {name}; write them by hand.");
            }

            List<(string Name, string Value)> members = [];
            string? projectionProblem = !projectable.ContainsKey(key)
                ? $"ProjectTo uses {name[..^"Projection".Length]}, whose destination is created through its constructor; write the projection by hand."
                : projections.Problem(key, out members) is string problem ? problem + "." : null;
            if (projectionProblem is not null)
            {
                if (problems is null)
                {
                    return PatternResult.Manual(projectionProblem);
                }

                problems.Add((statement, projectionProblem));
                continue;
            }

            withProjection.Add((source, destination, name, members, CapturedLocal.Used(captured, members)));
            namespaces.UnionWith(["System", "System.Linq.Expressions"]);
            if (members.Any(m => m.Value.Contains(".Select(", StringComparison.Ordinal)))
            {
                namespaces.Add("System.Linq");
            }
        }

        SyntaxNode original = (SyntaxNode?)constructor ?? lambda!;
        SyntaxNode rewritten;
        string? directiveProblem;
        if (lambda is null)
        {
            directiveProblem = ProfileDirectives.Repair(kept.Count == 0 ? MapMethodWriter.Emptied(constructor!) : MapMethodWriter.Trimmed(constructor!, kept, model), out ConstructorDeclarationSyntax repairedConstructor);
            rewritten = repairedConstructor;
        }
        else
        {
            directiveProblem = ProfileDirectives.Repair(kept.Count == 0 ? MapMethodWriter.Emptied(lambda) : MapMethodWriter.Trimmed(lambda, kept), out LambdaExpressionSyntax repairedLambda);
            rewritten = repairedLambda;
        }

        if (directiveProblem is not null)
        {
            return PatternResult.Manual(directiveProblem);
        }

        return PatternResult.Fixed([
            anchor.ReplaceNode(original, rewritten),
            MapMethodWriter.Extensions(anchor, className, isInternal ? "internal" : "public", maps, withProjection, captured)], [.. namespaces.Order(StringComparer.Ordinal)]);
    }

    // Why a profile with several constructors stays Manual (TAAK-266): which maps exist depends on the constructor the caller runs. The common
    // shape (dasblog) keeps the maps in the one constructor that takes services; the lambdas then read those services through parameters or
    // fields, which a static ToB method cannot reach (nor can it know which constructor the application calls).
    private static string SeveralConstructorsReason(ClassDeclarationSyntax profile, SemanticModel model)
    {
        List<ConstructorDeclarationSyntax> constructors = [.. profile.Members.OfType<ConstructorDeclarationSyntax>().Where(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword))];
        List<ConstructorDeclarationSyntax> withMaps = [.. constructors.Where(c => ProfileSyntax.MapStatements(c).Count > 0)];
        string reason = $"Profile {profile.Identifier} has several constructors (which maps exist depends on the one that runs)";
        if (withMaps is not [{ } holder])
        {
            return reason + "; write its mapping methods by hand.";
        }

        List<string> captured = [];
        foreach (SimpleNameSyntax name in holder.DescendantNodes().OfType<SimpleNameSyntax>().Where(MapAccessibility.IsReference))
        {
            if (model.GetSymbolInfo(name).Symbol is { IsStatic: false } symbol
                && ((symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } } && holder.ParameterList.Parameters.Any(p => p.Identifier.ValueText == symbol.Name))
                    || (symbol is IFieldSymbol or IMethodSymbol && symbol.ContainingType is { } owner && owner.Name == profile.Identifier.ValueText))
                && !captured.Contains(symbol.Name))
            {
                captured.Add(symbol.Name);
            }
        }

        return captured.Count == 0
            ? reason + $"; the maps are only in the constructor ({string.Join(", ", holder.ParameterList.Parameters.Select(p => p.ToString()))}), so the application has to call that one; write its mapping methods by hand."
            : reason + $"; the maps are in the constructor ({string.Join(", ", holder.ParameterList.Parameters.Select(p => p.ToString()))}) and read {string.Join(", ", captured)} "
                + "(a service handed in by the caller), which a static ToB method cannot reach; write its mapping methods by hand (pass the service as an argument).";
    }

    // Public instance members, the type's own first; a member hidden by a derived one counts once.
    internal static IEnumerable<ISymbol> AllMembers(ITypeSymbol type)
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

}
