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
    private const string AutoMapperAssembly = "AutoMapper";

    public virtual string Id => "automapper.create-map";

    public virtual string Description =>
        "Profile with CreateMap<A, B>() without options → static B ToB(this A source) => new() { X = source.X, ... } for the members with the same name and type (or a built-in conversion AutoMapper does the same way); MapInto(this A source, B destination) when the solution calls Map(source, destination); AToBProjection when the solution calls ProjectTo; Map with options is Manual";

    // Map and ProjectTo calls that MapCallPattern does not rewrite are found too: while they run, their CreateMap has to stay (TAAK-272).
    public virtual IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => n switch
        {
            ClassDeclarationSyntax c => IsProfileWithMaps(c, model) && !UsesForMember(c, ignore: true) && !UsesForMember(c, ignore: false),
            InvocationExpressionSyntax i => IsOtherMapCall(i, model),
            _ => false,
        }).Select(Anchor).Distinct();

    // Without the solution no MapInto is written: nothing says a Map(source, destination) call needs it.
    public virtual PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Rewrite(node, model, new HashSet<string>(), nested: null, projectTo: new HashSet<string>(), asserted: Asserts(model), MapOwners.None, problems: null);

    public virtual async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(solution);
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
        if (result is ManualResult { Reason: var reason } && !reason.StartsWith(MapOwners.KeptPrefix, StringComparison.Ordinal))
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
            return PatternResult.Manual(SeveralConstructorsReason(profile, model));
        }

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
        foreach (SyntaxNode statement in MapStatements(constructor))
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
        }

        if (maps.Count == 0 && keptReason is not null)
        {
            return PatternResult.Manual(MapOwners.KeptPrefix + keptReason);
        }


        // One CreateMap statement: its methods are added to maps, or the reason it cannot be rewritten.
        string? Statement(SyntaxNode statement)
        {
            if (!TryParseMap(statement, model, out InvocationExpressionSyntax? invocation, out Dictionary<string, SimpleLambdaExpressionSyntax?>? options,
                    out Dictionary<string, SimpleLambdaExpressionSyntax?>? reverseOptions, out Dictionary<string, InvocationExpressionSyntax> resolverCalls,
                    out InvocationExpressionSyntax? convertUsing, out Dictionary<string, InvocationExpressionSyntax> converters, out MemberOptions extra, out MemberOptions reverseExtra)
                || invocation.Expression is not GenericNameSyntax { Identifier.ValueText: "CreateMap", TypeArgumentList.Arguments: [{ } source, { } destination] }
                || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly } method)
            {
                string written = string.Join(' ', statement.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                return extra.Unsupported is { } unsupported
                    ? $"{unsupported}; migrate Profile {profile.Identifier} by hand."
                    : UnsupportedOptions.Reason(statement, model) is string advice
                        ? $"'{written}' in Profile {profile.Identifier}: {advice} (automapper.unsupported)."
                        : $"'{written}' is not a CreateMap<A, B>() without options; migrate Profile {profile.Identifier} by hand.";
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
                if (Converters.TypeProblem(convertUsing, sourceType, destinationType, MethodName(destination), model, profileType, out creation) is string converterProblem)
                {
                    return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: {converterProblem}; write the mapping method by hand.";
                }
            }
            else if (Problem(sourceType, destinationType, options, model, profileType, values, out members, out copied, out creation,
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

            Accessibility access = Weakest(Weakest(Effective(sourceType), Effective(destinationType)), Accessibility.Public);
            if (access is not (Accessibility.Public or Accessibility.Internal))
            {
                return $"{sourceType.Name} or {destinationType.Name} is private or protected; an extension method cannot use it.";
            }

            if ((OutsideProblem(options, model, anchor.SpanStart) ?? OutsideProblem(reverseOptions ?? [], model, anchor.SpanStart) ?? OutsideProblem(extra.Lambdas(), model, anchor.SpanStart) ?? OutsideProblem(reverseExtra.Lambdas(), model, anchor.SpanStart)
                ?? (extra.ConstructUsing is { ArgumentList.Arguments: [{ Expression: LambdaExpressionSyntax { Body: { } constructed } }] } ? OutsideProblem("ConstructUsing", constructed, model, anchor.SpanStart) : null)) is string outside)
            {
                return $"CreateMap<{sourceType.Name}, {destinationType.Name}>: {outside}; write the mapping method by hand.";
            }

            if (anchor != profile)
            {
                // A nested profile can name a type of its containing types without their names (Command for AddNote.Command).
                (string written, string writtenReverse) = (MethodName(destination), MethodName(source));
                (source, destination) = (AtAnchor(source, sourceType, model, anchor.SpanStart), AtAnchor(destination, destinationType, model, anchor.SpanStart));
                if (MethodName(destination) != written || (reverseOptions is not null && MethodName(source) != writtenReverse))
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
                && Reverse(sourceType, destinationType, options, reverseOptions, reverseExtra, model, profileType, values, mapInto, out reverseMembers,
                    out reverseMapInto, out reverseCreation) is string reverseProblem)
            {
                return $"ReverseMap of CreateMap<{sourceType.Name}, {destinationType.Name}>: {reverseProblem}; write the mapping method by hand.";
            }

            string key = MapIntoPattern.Key(sourceType, destinationType);
            string reverseKey = MapIntoPattern.Key(destinationType, sourceType);
            if (pairs.Contains(key) || (reverseOptions is not null && (pairs.Contains(reverseKey) || key == reverseKey)))
            {
                // Two maps for one type pair (a ReverseMap and its own CreateMap<B, A>) would be two methods with the same signature.
                return $"Profile {profile.Identifier} maps {sourceType.Name} and {destinationType.Name} the same way twice (ReverseMap and a CreateMap of its own?); write the mapping methods by hand.";
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

        string className = ExtensionsName(profile);
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

        return PatternResult.Fixed([
            anchor.ReplaceNode(constructor, kept.Count == 0 ? Emptied(constructor) : Trimmed(constructor, kept, model)),
            Extensions(anchor, className, isInternal ? "internal" : "public", maps, withProjection, captured)], [.. namespaces.Order(StringComparer.Ordinal)]);
    }

    // Why a profile with several constructors stays Manual (TAAK-266): which maps exist depends on the constructor the caller runs. The common
    // shape (dasblog) keeps the maps in the one constructor that takes services; the lambdas then read those services through parameters or
    // fields, which a static ToB method cannot reach (nor can it know which constructor the application calls).
    private static string SeveralConstructorsReason(ClassDeclarationSyntax profile, SemanticModel model)
    {
        List<ConstructorDeclarationSyntax> constructors = [.. profile.Members.OfType<ConstructorDeclarationSyntax>().Where(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword))];
        List<ConstructorDeclarationSyntax> withMaps = [.. constructors.Where(c => MapStatements(c).Count > 0)];
        string reason = $"Profile {profile.Identifier} has several constructors (which maps exist depends on the one that runs)";
        if (withMaps is not [{ } holder])
        {
            return reason + "; write its mapping methods by hand.";
        }

        List<string> captured = [];
        foreach (SimpleNameSyntax name in holder.DescendantNodes().OfType<SimpleNameSyntax>().Where(IsReference))
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

    // The constructor with only the maps that stay on AutoMapper (TAAK-272), and the captured variables they read.
    private static ConstructorDeclarationSyntax Trimmed(ConstructorDeclarationSyntax constructor, List<SyntaxNode> kept, SemanticModel model)
    {
        HashSet<string> read = [.. kept.SelectMany(k => k.DescendantNodes().OfType<IdentifierNameSyntax>()).Select(n => n.Identifier.ValueText)];
        return constructor.WithBody(constructor.Body!.WithStatements([.. constructor.Body.Statements
            .Where(s => kept.Contains(s) || (CapturedLocal.TryParse(s, model) is { } local && read.Contains(local.Name)))]));
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
            if (lambda is not null && OutsideProblem("MapFrom for " + member, lambda.Body, model, position) is string problem)
            {
                return problem;
            }
        }

        return null;
    }

    // The same check for any expression that moves (what: "MapFrom for X", "ConstructUsing").
    private static string? OutsideProblem(string what, SyntaxNode body, SemanticModel model, int position)
    {
        foreach (SimpleNameSyntax name in body.DescendantNodesAndSelf().OfType<SimpleNameSyntax>())
        {
            if (name.Parent is NameColonSyntax or NameEqualsSyntax || model.GetSymbolInfo(name).Symbol is not { } symbol
                || symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol)
            {
                continue;
            }

            if (!model.Compilation.IsSymbolAccessibleWithin(symbol is IMethodSymbol { ReducedFrom: { } extension } ? extension : symbol, model.Compilation.Assembly))
            {
                return $"{what} uses {name.Identifier.ValueText}, which is private or protected";
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
                return $"{what} uses {name.Identifier.ValueText}, which means something else outside the type around the profile";
            }
        }

        return null;
    }

    // Why the map cannot be written as plain assignments, or null with the destination members AutoMapper writes (and what to assign),
    // each having a source member of the same name and type (or one MemberValues maps) or a MapFrom expression. copied: the first member
    // that gets a new object or collection. reversed (only for the reverse map of a ReverseMap): destination member → source member of a
    // reversed MapFrom; a destination member without any source is then left alone instead of Manual.
    private static string? Problem(ITypeSymbol source, ITypeSymbol destination, Dictionary<string, SimpleLambdaExpressionSyntax?> options,
        SemanticModel model, INamedTypeSymbol? profileType, MemberValues values, out List<(string Name, string Value)> members, out string? copied, out string creation,
        Dictionary<string, string>? reversed = null, ValueResolvers? resolvers = null, List<ProjectionMember>? read = null,
        Dictionary<string, InvocationExpressionSyntax>? converters = null, MemberOptions? extra = null, bool asserted = false)
    {
        converters ??= [];
        // read: what AutoMapper reads for each assigned member, before the ToB-only rewrites (a projection uses it).
        members = [];
        copied = null;
        creation = "new()";
        if (destination is not INamedTypeSymbol { IsAbstract: false } named || destination.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return $"{destination.Name} has no public parameterless constructor (AutoMapper would map constructor parameters)";
        }

        List<ISymbol> readable = [.. AllMembers(source).Where(m => m switch
        {
            IPropertySymbol p => !p.IsIndexer && p.GetMethod?.DeclaredAccessibility == Accessibility.Public,
            IFieldSymbol => true,
            _ => false,
        })];

        // ConstructUsing (TAAK-266) makes the destination through its own expression, so no constructor of the type is chosen.
        string? constructUsing = null;
        bool plainCreation = false;
        IMethodSymbol? constructor = null;
        if (extra?.ConstructUsing is { } constructCall)
        {
            if (ConstructUsingProblem(constructCall, destination, model, profileType, out constructUsing, out plainCreation) is string constructProblem)
            {
                return constructProblem;
            }
        }
        else if (destination.TypeKind == TypeKind.Class && ConstructorChoice(named, source, readable, model, extra?.CtorParams, out constructor) is string choiceProblem)
        {
            return choiceProblem;
        }
        List<ISymbol> destinationMembers = [.. AllMembers(destination).Where(m => m is IPropertySymbol { IsIndexer: false } or IFieldSymbol { IsConst: false })];
        foreach ((string name, bool mapFrom) in options.Select(o => (o.Key, o.Value is not null)).Concat((resolvers?.Members ?? []).Select(m => (m, true))))
        {
            ISymbol? configured = destinationMembers.FirstOrDefault(m => m.Name == name);
            if (configured is null)
            {
                // AutoMapper rejects an unknown member in the configuration; there is nothing sound to leave out.
                return $"ForMember configures {name}, which {destination.Name} does not have as a public property or field";
            }

            if (mapFrom && !(configured is IFieldSymbol { IsReadOnly: false } || configured is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public }))
            {
                return $"ForMember maps {destination.Name}.{name}, which has no public setter";
            }
        }

        if (extra is { CtorParams.Count: > 0 } && constructor is null)
        {
            return $"ForCtorParam configures a constructor parameter, but {destination.Name} is made through ConstructUsing or its parameterless constructor";
        }

        if ((constructor is not null || constructUsing is not null) && extra is { Conditions.Count: > 0 } or { Paths.Count: > 0 })
        {
            return $"{destination.Name} is created through its constructor or ConstructUsing, which may set a member a Condition or ForPath keeps or fills (TAAK-255)";
        }

        if (converters.Keys.FirstOrDefault(name => !destinationMembers.Any(m => m.Name == name
            && (m is IFieldSymbol { IsReadOnly: false } || m is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public }))) is { } unwritable)
        {
            return $"ForMember converts {destination.Name}.{unwritable}, which is not a public property with a setter or a field";
        }

        if (constructor is not null && converters.Count > 0)
        {
            return $"{destination.Name} is made through a constructor and ForMember uses a value converter; write the mapping method by hand";
        }

        if (constructor is not null && ConstructorProblem(constructor, source, destination, options, readable, values, extra?.CtorParams, model, profileType, out creation, ref copied) is string constructorProblem)
        {
            return constructorProblem;
        }

        foreach (ISymbol target in destinationMembers.Where(m => m is IPropertySymbol or IFieldSymbol { IsReadOnly: false }))
        {
            // A member a constructor parameter of the same name (any case) filled is not mapped again (measured, TAAK-251).
            if (constructor is not null && constructor.Parameters.Any(p => string.Equals(p.Name, target.Name, StringComparison.OrdinalIgnoreCase)))
            {
                if (resolvers?.Has(target.Name) == true)
                {
                    return $"a value resolver configures {target.Name}, which a constructor parameter of {destination.Name} also fills";
                }

                continue;
            }

            if (resolvers?.Has(target.Name) == true)
            {
                ITypeSymbol resolvedType = target is IPropertySymbol resolvedProperty ? resolvedProperty.Type : ((IFieldSymbol)target).Type;
                if (resolvers.Problem(target.Name, resolvedType, destination, values, out string resolved, out bool resolvedCopies) is string resolverProblem)
                {
                    return resolverProblem;
                }

                members.Add((target.Name, resolved));
                copied ??= resolvedCopies ? target.Name : null;
                continue;
            }

            // ConvertUsing(converter[, s => s.Y]): X = converter.Convert(source.Y, null) (TAAK-253).
            if (converters.TryGetValue(target.Name, out InvocationExpressionSyntax? converter))
            {
                if (Converters.MemberProblem(converter, target.Name, source, model, profileType, out string converted) is string converterProblem)
                {
                    return converterProblem;
                }

                members.Add((target.Name, converted));
                continue;
            }

            if (extra is not null && extra.Paths.TryGetValue(target.Name, out List<(string Member, SimpleLambdaExpressionSyntax MapFrom)>? path))
            {
                if (PathProblem(target, path, source, options, extra, model, profileType, values, out string created) is string pathProblem)
                {
                    return pathProblem;
                }

                members.Add((target.Name, created));
                continue;
            }

            // Ignore(): AutoMapper leaves the member alone, so the new object does too (also when the source has a member of that name).
            if (options.TryGetValue(target.Name, out SimpleLambdaExpressionSyntax? mapFromLambda))
            {
                if (mapFromLambda is null)
                {
                    continue;
                }

                ITypeSymbol targetType = target is IPropertySymbol property ? property.Type : ((IFieldSymbol)target).Type;
                if (MapFromProblem(target.Name, mapFromLambda, model, profileType, targetType, destination, values, nullSafe: true, out string value, out bool copies,
                    out string expression, out ITypeSymbol? expressionType, out string? readProblem) is string mapFromProblem)
                {
                    return mapFromProblem;
                }

                members.Add((target.Name, value));
                read?.Add(new ProjectionMember { Name = target.Name, Value = expression, From = expressionType!, To = targetType, Problem = readProblem });
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
                // Measured (TAAK-261): when the source has the member, AutoMapper writes a non-public setter and fills a get-only collection
                // (clears it, adds the source items); a get-only member of another type (an array, a string, an object) it leaves alone.
                if (MayFlatten(target.Name, source) && target is IPropertySymbol { SetMethod: not null })
                {
                    return $"{destination.Name}.{target.Name} has a non-public setter, which AutoMapper writes from {source.Name}";
                }

                if (MayFlatten(target.Name, source) && IsFilledCollection(target is IPropertySymbol gp ? gp.Type : ((IFieldSymbol)target).Type))
                {
                    return $"{destination.Name}.{target.Name} is a get-only collection, which AutoMapper fills with the items from {source.Name}";
                }

                continue;
            }

            ITypeSymbol to = target is IPropertySymbol tp ? tp.Type : ((IFieldSymbol)target).Type;
            ITypeSymbol from;
            string reads;
            string plain;
            ITypeSymbol plainType;
            if (matches is [{ } match] && match.Name == target.Name)
            {
                from = match is IPropertySymbol fp ? fp.Type : ((IFieldSymbol)match).Type;
                reads = "source." + target.Name;
                plain = reads;
                plainType = from;
            }
            else if (reversed is not null)
            {
                return $"{destination.Name}.{target.Name} has no member with the same name in {source.Name}";
            }
            else if (Flattening.Problem(source, destination, target.Name, out IReadOnlyList<ISymbol> found) is null && found.Count == 0)
            {
                // Measured (TAAK-261): AutoMapper leaves a member without a source alone, so the new object keeps its initializer or default.
                // Only AssertConfigurationIsValid rejects that; when the solution calls it, the member must be mapped in a way this fix does not see.
                if (asserted)
                {
                    return $"{destination.Name}.{target.Name} has no member with the same name in {source.Name}, nor one AutoMapper flattens or reads through a Get method, "
                        + "yet the solution calls AssertConfigurationIsValid, which rejects such a member";
                }

                continue;
            }
            else if (FlattenedProblem(source, destination, target.Name, to, values, out reads, out from, out plain, out plainType) is string flattened)
            {
                // AutoMapper's other name rules (TAAK-256): any case, a Get prefix, flattening (Customer.Name → CustomerName).
                return flattened;
            }

            if (values.Problem(target.Name, reads, from, to, destination, out string? assigned, out bool copy) is string valueProblem)
            {
                return valueProblem;
            }

            if (assigned is null)
            {
                return $"{target.Name} is {from.Name} in {source.Name} but {to.Name} in {destination.Name} (AutoMapper converts){MemberValues.NoMap(from, to)}";
            }

            members.Add((target.Name, assigned));
            // ProjectTo reads a flattened path without null checks (TAAK-252: the query has none).
            read?.Add(new ProjectionMember { Name = target.Name, Value = plain, From = plainType, To = to });
            copied ??= copy ? target.Name : null;
        }

        if (constructUsing is not null)
        {
            // The expression runs first, then the members are assigned: onto a plain `new T(...)` as its initializer, otherwise in a block body.
            creation = members.Count == 0 || plainCreation ? constructUsing : BlockMarker + constructUsing;
        }

        return extra is null ? null : ConditionProblem(destination, extra, model, profileType, values, members);
    }

    // Starts the creation text of a map that needs a block body (var destination = expression; destination.X = ...; return destination;).
    private const char BlockMarker = '\u0001';

    // Measured (TAAK-266, AutoMapper 14.0.0): AutoMapper takes the constructor with the most parameters it can resolve (a parameter with a source member
    // of the same name in any case, flattened or read through a Get method, or an optional one), the first declared on a tie, over a parameterless one
    // and whatever its accessibility (private ones too); it throws when none can be resolved. Why that choice cannot be frozen into one new(...) call, or null
    // with the chosen constructor (null: the public parameterless one, `new()`).
    private static string? ConstructorChoice(INamedTypeSymbol destination, ITypeSymbol source,
        List<ISymbol> readable, SemanticModel model, Dictionary<string, SimpleLambdaExpressionSyntax>? ctorParams, out IMethodSymbol? chosen)
    {
        chosen = null;
        // The record copy constructor does not count.
        List<IMethodSymbol> candidates = [.. destination.InstanceConstructors.Where(c => !(c.IsImplicitlyDeclared && c.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, destination)))];
        if (candidates is [{ DeclaredAccessibility: Accessibility.Public } only])
        {
            // The common case: its parameters are checked by ConstructorProblem.
            chosen = only.Parameters.Length == 0 ? null : only;
            return null;
        }

        if (candidates.Count == 0 || candidates is [var lone] && lone.DeclaredAccessibility != Accessibility.Public)
        {
            return $"{destination.Name} has no public constructor (AutoMapper uses the one with the most parameters it can resolve, also a private one)";
        }

        IMethodSymbol? winner = null;
        List<IMethodSymbol> sameCount = [];
        foreach (IMethodSymbol candidate in candidates.OrderByDescending(c => c.Parameters.Length))
        {
            if (winner is not null && candidate.Parameters.Length < winner.Parameters.Length)
            {
                break;
            }

            if (candidate.Parameters.FirstOrDefault(p => p.RefKind != RefKind.None || p.IsParams) is { } odd)
            {
                return $"a constructor of {destination.Name} has a ref, out or params parameter {odd.Name}";
            }

            bool resolvable = true;
            foreach (IParameterSymbol parameter in candidate.Parameters)
            {
                // A ForMember on the member of that name does not make the parameter resolvable (measured, TAAK-266).
                int matches = readable.Count(m => string.Equals(m.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
                if (matches > 1)
                {
                    return $"the constructor parameter {parameter.Name} of {destination.Name} matches several members of {source.Name}";
                }

                if (matches != 1 && ctorParams?.ContainsKey(parameter.Name) == true)
                {
                    return $"{destination.Name} has several constructors and ForCtorParam fills the parameter {parameter.Name}, which has no single member of that name in {source.Name}; which constructor AutoMapper picks is not decided here";
                }

                if (matches == 0 && MayFlatten(parameter.Name, source))
                {
                    return $"{destination.Name} has several constructors and AutoMapper may fill the parameter {parameter.Name} of one by flattening or a Get method of {source.Name}; which one it picks is not decided here";
                }

                resolvable &= matches == 1 || parameter.HasExplicitDefaultValue;
            }

            if (resolvable)
            {
                winner ??= candidate;
                sameCount.Add(candidate);
            }
        }

        if (winner is null)
        {
            return $"AutoMapper can resolve no constructor of {destination.Name} from {source.Name} (it throws)";
        }

        if (sameCount.Count > 1 && sameCount.Select(c => c.Locations.FirstOrDefault()?.SourceTree).Distinct().Count() > 1)
        {
            return $"{destination.Name} has several constructors with {winner.Parameters.Length} parameters AutoMapper can resolve, declared in different files (the first declared wins, which is not decided here)";
        }

        // sameCount holds them in the order AutoMapper tries (declaration order).
        winner = sameCount[0];
        if (winner.Parameters.Length == 0)
        {
            return winner.DeclaredAccessibility == Accessibility.Public
                ? null
                : $"AutoMapper would create {destination.Name} through its parameterless constructor, which is not public";
        }

        if (!model.Compilation.IsSymbolAccessibleWithin(winner, model.Compilation.Assembly))
        {
            return $"AutoMapper picks the constructor ({string.Join(", ", winner.Parameters.Select(p => p.Name))}) of {destination.Name}, which is private or protected; the extension method cannot call it";
        }

        chosen = winner;
        return null;
    }

    // Why `ConstructUsing(s => expression)` cannot become the creation of the new object, or null with that expression over the name source.
    // Measured (TAAK-266): the destination is made by the expression instead of a constructor of the type (also one the type has no parameterless
    // for), then every member is mapped as without it: a settable member is overwritten from its source member, one without a source keeps what the
    // expression gave it, Ignore leaves it, MapFrom wins, a get-only member is not written; the expression runs unguarded (a null inside throws).
    private static string? ConstructUsingProblem(InvocationExpressionSyntax call, ITypeSymbol destination, SemanticModel model, INamedTypeSymbol? profileType,
        out string? text, out bool plain)
    {
        text = null;
        plain = false;
        if (call.ArgumentList.Arguments is not [{ Expression: LambdaExpressionSyntax lambda, NameColon: null, RefKindKeyword.RawKind: 0 }])
        {
            return "ConstructUsing is not given a lambda (a method group or ConstructUsing<TServiceConstructor>()); write the construction by hand";
        }

        ParameterSyntax? parameterSyntax = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [{ } only] } => only,
            _ => null,
        };
        if (parameterSyntax is null)
        {
            return "ConstructUsing takes a ResolutionContext (ctx.Mapper, ctx.Items), which only exists while AutoMapper maps; write the construction by hand";
        }

        if (lambda.Body is not ExpressionSyntax body || lambda.AsyncKeyword != default || model.GetDeclaredSymbol(parameterSyntax) is not IParameterSymbol parameter)
        {
            return "ConstructUsing has a lambda without an expression body; write the construction by hand";
        }

        if (destination is not INamedTypeSymbol { IsAbstract: false } || destination.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return $"{destination.Name} is abstract or no class or struct; ConstructUsing may return a derived type, write the construction by hand";
        }

        List<SimpleNameSyntax> parameterUses = [];
        foreach (SyntaxNode node in body.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case ThisExpressionSyntax or BaseExpressionSyntax:
                    return "ConstructUsing uses this or base of the profile";
                case SimpleNameSyntax name when IsReference(name):
                    ISymbol? symbol = model.GetSymbolInfo(name).Symbol;
                    if (SymbolEqualityComparer.Default.Equals(symbol, parameter))
                    {
                        parameterUses.Add(name);
                    }
                    else if (name.Identifier.ValueText is "source" or "destination" && symbol is not INamedTypeSymbol)
                    {
                        return $"ConstructUsing already uses the name {name.Identifier.ValueText} for something else";
                    }
                    else if (!IsAvailableOutsideProfile(symbol, body, profileType))
                    {
                        return $"ConstructUsing uses {name.Identifier.ValueText}, which is not available outside the profile (a constructor parameter, field or private member of it)";
                    }

                    break;
            }
        }

        if (model.GetTypeInfo(body).Type is not { TypeKind: not TypeKind.Error })
        {
            return "ConstructUsing returns an expression without a type of its own (a null literal, for one)";
        }

        ExpressionSyntax replaced = parameter.Name == "source"
            ? body
            : body.ReplaceNodes(parameterUses, (original, _) => SyntaxFactory.IdentifierName("source").WithTriviaFrom(original));
        text = replaced.WithoutTrivia().ToString();
        plain = body is BaseObjectCreationExpressionSyntax { Initializer: null };
        return null;
    }

    // Why a ForPath(d => d.A.B, o => o.MapFrom(...)) group cannot be written as A = new() { B = ... }, or null with that value in created.
    // Measured (TAAK-255): on a new object AutoMapper creates A with its parameterless constructor (initializers run) even when every
    // value is null, and a ForPath MapFrom is null-safe like a ForMember one.
    private static string? PathProblem(ISymbol target, List<(string Member, SimpleLambdaExpressionSyntax MapFrom)> path, ITypeSymbol source,
        Dictionary<string, SimpleLambdaExpressionSyntax?> options, MemberOptions extra, SemanticModel model, INamedTypeSymbol? profileType, MemberValues values, out string created)
    {
        created = "";
        ITypeSymbol type = target is IPropertySymbol property ? property.Type : ((IFieldSymbol)target).Type;
        if (!(target is IFieldSymbol { IsReadOnly: false } || target is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public }))
        {
            return $"ForPath writes into {target.Name}, which has no public setter (AutoMapper fills the object it already holds)";
        }

        if (options.ContainsKey(target.Name) || extra.Conditions.ContainsKey(target.Name) || MayFlatten(target.Name, source))
        {
            return $"ForPath writes into {target.Name}, which AutoMapper also maps itself (from a member of {source.Name} or a ForMember option)";
        }

        if (type is not INamedTypeSymbol { IsAbstract: false, TypeKind: TypeKind.Class } named
            || !named.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
        {
            return $"ForPath writes into {target.Name}, whose type {type.Name} is not a class with a public parameterless constructor";
        }

        List<string> assignments = [];
        foreach ((string member, SimpleLambdaExpressionSyntax mapFrom) in path)
        {
            if (AllMembers(named).FirstOrDefault(m => m.Name == member) is not ({ } inner and (IFieldSymbol { IsReadOnly: false } or IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public })))
            {
                return $"ForPath to {target.Name}.{member}: {type.Name} has no public settable member {member}";
            }

            ITypeSymbol innerType = inner is IPropertySymbol innerProperty ? innerProperty.Type : ((IFieldSymbol)inner).Type;
            if (MapFromProblem(target.Name + "." + member, mapFrom, model, profileType, innerType, named, values, nullSafe: false, out string value, out _, out _, out _, out _) is string problem)
            {
                return problem;
            }

            assignments.Add($"{member} = {value}");
        }

        created = $"new() {{ {string.Join(", ", assignments)} }}";
        return null;
    }

    // Why a Condition or PreCondition cannot become X = cond ? value : default, or null with the members rewritten that way. Measured (TAAK-255):
    // when the condition is false AutoMapper leaves the member at the value the new object has (its initializer or default); Condition
    // runs after the value is resolved (a MapFrom still runs), PreCondition before; a condition that dereferences null throws.
    private static string? ConditionProblem(ITypeSymbol destination, MemberOptions extra, SemanticModel model, INamedTypeSymbol? profileType, MemberValues values,
        List<(string Name, string Value)> members)
    {
        ITypeSymbol boolean = model.Compilation.GetSpecialType(SpecialType.System_Boolean);
        foreach ((string name, SimpleLambdaExpressionSyntax? lambda) in extra.Conditions)
        {
            int index = members.FindIndex(m => m.Name == name);
            if (index < 0 || lambda is null)
            {
                return $"the Condition for {name} is on a member the map does not write (Ignore or no public setter)";
            }

            ISymbol member = AllMembers(destination).First(m => m.Name == name);
            if (!KeepsDefault(member, destination))
            {
                return $"{destination.Name}.{name} has an initializer or {destination.Name} a constructor of its own: when the Condition is false AutoMapper keeps that value, which an object initializer cannot";
            }

            if (MapFromProblem("Condition of " + name, lambda, model, profileType, boolean, destination, values, nullSafe: false, out string condition, out _, out _, out _, out _) is string problem)
            {
                return problem;
            }

            ITypeSymbol type = member is IPropertySymbol property ? property.Type : ((IFieldSymbol)member).Type;
            string fallback = type.IsValueType || type.NullableAnnotation == NullableAnnotation.Annotated ? "default" : "default!";
            members[index] = (name, $"{Grouped(condition)} ? {Grouped(members[index].Value)} : {fallback}");
        }

        return null;
    }

    // Why a member without a source member of its exact name cannot be read the way AutoMapper reads it, or null with the expression in read.
    private static string? FlattenedProblem(ITypeSymbol source, ITypeSymbol destination, string name, ITypeSymbol to, MemberValues values,
        out string read, out ITypeSymbol from, out string plain, out ITypeSymbol plainType)
    {
        read = "";
        from = to;
        plain = "";
        plainType = to;
        if (Flattening.Problem(source, destination, name, out IReadOnlyList<ISymbol> path) is string problem)
        {
            return problem;
        }

        if (path.Count == 0)
        {
            return $"{destination.Name}.{name} has no member with the same name in {source.Name}, nor one AutoMapper flattens or reads through a Get method";
        }

        plain = "source" + string.Concat(path.Select(m => "." + m.Name + (m is IMethodSymbol ? "()" : "")));
        plainType = path[^1] switch { IPropertySymbol p => p.Type, IFieldSymbol f => f.Type, _ => ((IMethodSymbol)path[^1]).ReturnType };
        return Flattening.Value(path, to, values.NullableWarnings, out read, out from) is string valueProblem
            ? $"{destination.Name}.{name}: {valueProblem}"
            : null;
    }

    // Whether a new destination holds the default value in member before mapping: no initializer and no constructor declared on the way up.
    private static bool KeepsDefault(ISymbol member, ITypeSymbol destination)
    {
        if (member.DeclaringSyntaxReferences.Length == 0 || member.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is PropertyDeclarationSyntax { Initializer: not null } or VariableDeclaratorSyntax { Initializer: not null } or ParameterSyntax))
        {
            return false;
        }

        for (ITypeSymbol? current = destination; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            if (current is not INamedTypeSymbol named || named.InstanceConstructors.Any(c => !c.IsImplicitlyDeclared) || current.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }
        }

        return true;
    }

    // An expression that may not stand bare as a part of cond ? a : b.
    private static string Grouped(string expression) =>
        SyntaxFactory.ParseExpression(expression) is ConditionalExpressionSyntax or AssignmentExpressionSyntax or LambdaExpressionSyntax or SwitchExpressionSyntax
            ? $"({expression})"
            : expression;

    // Why the single constructor cannot be called as new(source.X, ...), or null with that call in creation. AutoMapper fills each parameter
    // from the source member of the same name in any case (measured, TAAK-251); a parameter without one gets its default value or makes Map throw.
    private static string? ConstructorProblem(IMethodSymbol constructor, ITypeSymbol source, ITypeSymbol destination,
        Dictionary<string, SimpleLambdaExpressionSyntax?> options, List<ISymbol> readable, MemberValues values,
        Dictionary<string, SimpleLambdaExpressionSyntax>? ctorParams, SemanticModel model, INamedTypeSymbol? profileType, out string creation, ref string? copied)
    {
        creation = "";
        List<string> arguments = [];
        foreach (IParameterSymbol parameter in constructor.Parameters)
        {
            if (parameter.RefKind != RefKind.None || parameter.IsParams)
            {
                return $"the constructor of {destination.Name} has a ref, out or params parameter {parameter.Name}";
            }

            if (options.Keys.FirstOrDefault(k => string.Equals(k, parameter.Name, StringComparison.OrdinalIgnoreCase)) is { } configured)
            {
                return $"ForMember configures {configured}, which the constructor parameter {parameter.Name} of {destination.Name} fills (AutoMapper may apply it to the parameter)";
            }

            if (ctorParams?.TryGetValue(parameter.Name, out SimpleLambdaExpressionSyntax? ctorLambda) == true)
            {
                // Measured (TAAK-270, AutoMapper 14.0.0): the MapFrom of ForCtorParam is null-safe like a member's MapFrom.
                if (MapFromProblem("ForCtorParam " + parameter.Name, ctorLambda, model, profileType, parameter.Type, destination, values, nullSafe: true, out string fromParameter,
                    out bool parameterCopies, out _, out _, out _) is string ctorProblem)
                {
                    return ctorProblem;
                }

                arguments.Add(fromParameter);
                copied ??= parameterCopies ? parameter.Name : null;
                continue;
            }

            List<ISymbol> matches = [.. readable.Where(m => string.Equals(m.Name, parameter.Name, StringComparison.OrdinalIgnoreCase))];
            if (matches is not [{ } match])
            {
                return matches.Count == 0
                    ? $"the constructor parameter {parameter.Name} of {destination.Name} has no member with the same name in {source.Name} (AutoMapper flattens, uses the default value or throws)"
                    : $"the constructor parameter {parameter.Name} of {destination.Name} matches several members of {source.Name}";
            }

            ITypeSymbol from = match is IPropertySymbol property ? property.Type : ((IFieldSymbol)match).Type;
            if (values.Problem(parameter.Name, "source." + match.Name, from, parameter.Type, destination, out string? argument, out bool copy) is string valueProblem)
            {
                return valueProblem;
            }

            if (argument is null)
            {
                return $"the constructor parameter {parameter.Name} is {parameter.Type.Name} but {match.Name} is {from.Name} in {source.Name} (AutoMapper converts){MemberValues.NoMap(from, parameter.Type)}";
            }

            arguments.Add(argument);
            copied ??= copy ? parameter.Name : null;
        }

        if (ctorParams?.Keys.FirstOrDefault(k => !constructor.Parameters.Any(p => string.Equals(p.Name, k, StringComparison.OrdinalIgnoreCase))) is { } unknown)
        {
            return $"ForCtorParam names {unknown}, which is no parameter of the constructor AutoMapper uses for {destination.Name}";
        }

        creation = $"new({string.Join(", ", arguments)})";
        return null;
    }

    // Why the reverse map B → A of CreateMap<A, B>(forward).ReverseMap() cannot be written as plain assignments, or null with its members.
    // Measured on AutoMapper 14 (TAAK-249): the reverse map has MemberList.None and maps same-named members like any map; a forward Ignore
    // does not carry over; a forward MapFrom(s => s.X) for Y becomes X = source.Y (a longer path is unflattened, s => s maps into A itself);
    // other MapFrom expressions are not reversed; ReverseMap().ForMember options configure the reverse map and win over a reversed MapFrom;
    // a forward member AutoMapper flattened is unflattened.
    private static string? Reverse(ITypeSymbol source, ITypeSymbol destination, Dictionary<string, SimpleLambdaExpressionSyntax?> forward,
        Dictionary<string, SimpleLambdaExpressionSyntax?> options, MemberOptions extra, SemanticModel model, INamedTypeSymbol? profileType, MemberValues values,
        IReadOnlySet<string> mapInto, out List<(string Name, string Value)> members, out bool needsMapInto, out string creation)
    {
        members = [];
        creation = "new()";
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

        if (needsMapInto && (!extra.IsEmpty || extra.HasForPath))
        {
            return "a Condition or ForPath is not migrated together with Map(source, destination) (MapInto)";
        }

        // Measured (TAAK-264): a Condition acts on a reversed value like on any other; a ForPath next to a reversed MapFrom of the same member was not measured.
        if (extra.Paths.Keys.FirstOrDefault(reversed.ContainsKey) is { } pathTarget)
        {
            return $"ForPath writes into {pathTarget}, which the reversed MapFrom of the forward map also fills";
        }

        if (Problem(destination, source, options, model, profileType, values, out members, out string? copied, out creation, reversed, extra: extra) is string problem)
        {
            return problem;
        }

        // Not measured how a reversed MapFrom reaches a constructor parameter, nor can MapInto set one (TAAK-251).
        if (creation != "new()" && (reversed.Count > 0 || needsMapInto))
        {
            return $"{source.Name} is created through its constructor, which a reversed MapFrom or MapInto cannot be applied to";
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

    // Whether AutoMapper fills a get-only member of this type with the source items (measured on List and IReadOnlyList, TAAK-261):
    // any enumerable except an array or a string.
    private static bool IsFilledCollection(ITypeSymbol type) =>
        type is not IArrayTypeSymbol && type.SpecialType != SpecialType.System_String
        && (type.SpecialType == SpecialType.System_Collections_IEnumerable || type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable));

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
        ITypeSymbol targetType, ITypeSymbol destination, MemberValues values, bool nullSafe, out string value, out bool copies,
        out string read, out ITypeSymbol? readType, out string? readProblem)
    {
        readProblem = null;
        value = "";
        copies = false;
        read = "";
        readType = null;
        if (lambda.Body is not ExpressionSyntax body || model.GetDeclaredSymbol(lambda.Parameter) is not IParameterSymbol parameter || lambda.AsyncKeyword != default)
        {
            return $"MapFrom for {member} is not a lambda with an expression body";
        }

        string parameterName = parameter.Name;
        string? nullProblem = null;
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
                    else if (!(symbol is not null && values.Captured.Contains(symbol)) && !IsAvailableOutsideProfile(symbol, body, profileType))
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

            if (receiver is not null && nullProblem is null && MayBeNull(receiver, model, parameter))
            {
                nullProblem = $"MapFrom for {member} dereferences {receiver.ToString().Trim()}, which may be null (AutoMapper returns the default value, a plain assignment throws)";
            }
        }

        // A plain chain of members is null-safe with ?. (and ?? default for a value type): the same default AutoMapper returns (TAAK-263).
        string? chain = null;
        if (nullProblem is not null)
        {
            chain = nullSafe ? NullSafeChain(body, model, parameter) : null;
            if (chain is null)
            {
                return nullProblem;
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
        read = value;
        readType = from;
        bool nullable = from.IsValueType && from.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        if (chain is not null)
        {
            readProblem = nullProblem!.Replace("a plain assignment throws", "an expression tree throws", StringComparison.Ordinal);
            value = from.IsValueType && !nullable ? chain + " ?? default"
                : values.NullableWarnings && targetType.IsReferenceType && targetType.NullableAnnotation == NullableAnnotation.NotAnnotated ? chain + "!" : chain;
        }

        string plainValue = value;
        if (values.Problem("MapFrom for " + member, value, from, targetType, destination, out string? mapped, out copies) is string valueProblem)
        {
            return valueProblem;
        }

        if (chain is not null && mapped is not null && mapped != plainValue)
        {
            return nullProblem;
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

    // `source.A?.B.C`: body (a plain chain of properties and fields from the lambda parameter) over the name source, with ?. after every
    // receiver that may be null, or null when body is anything else.
    private static string? NullSafeChain(ExpressionSyntax body, SemanticModel model, IParameterSymbol parameter)
    {
        List<MemberAccessExpressionSyntax> links = [];
        ExpressionSyntax current = Unwrap(body);
        while (current is MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } access
            && model.GetSymbolInfo(access).Symbol is IPropertySymbol { IsStatic: false } or IFieldSymbol { IsStatic: false })
        {
            links.Insert(0, access);
            current = Unwrap(access.Expression);
        }

        if (links.Count == 0 || current is not IdentifierNameSyntax name || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name).Symbol, parameter))
        {
            return null;
        }

        StringBuilder chain = new("source");
        foreach (MemberAccessExpressionSyntax link in links)
        {
            chain.Append(MayBeNull(link.Expression, model, parameter) ? "?." : ".").Append(link.Name.Identifier.ValueText);
        }

        return chain.ToString();
    }

    // A name that is read (not the right side of a.b, an initializer member, a named argument or a property name of an anonymous type).
    internal static bool IsReference(SimpleNameSyntax name) => name.Parent switch
    {
        MemberAccessExpressionSyntax access => access.Expression == name,
        MemberBindingExpressionSyntax => false,
        NameColonSyntax or NameEqualsSyntax => false,
        AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment => assignment.Left != name,
        _ => true,
    };

    internal static bool IsAvailableOutsideProfile(ISymbol? symbol, ExpressionSyntax body, INamedTypeSymbol? profileType) => symbol switch
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
        [NotNullWhen(true)] out Dictionary<string, SimpleLambdaExpressionSyntax?>? options, out Dictionary<string, SimpleLambdaExpressionSyntax?>? reverse,
        out Dictionary<string, InvocationExpressionSyntax> resolvers, out InvocationExpressionSyntax? convertUsing, out Dictionary<string, InvocationExpressionSyntax> converters,
        out MemberOptions extra, out MemberOptions reverseExtra)
    {
        createMap = null;
        options = [];
        reverseExtra = new MemberOptions();
        reverse = null;
        extra = new MemberOptions();
        resolvers = [];
        convertUsing = null;
        converters = [];
        if (((statement as ExpressionStatementSyntax)?.Expression ?? (statement as ArrowExpressionClauseSyntax)?.Expression) is not InvocationExpressionSyntax expression)
        {
            return false;
        }

        // CreateMap<A, B>().ConvertUsing(...): the converter replaces the whole map, so nothing else may be configured (Converters checks the form).
        if (expression is { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConvertUsing", Expression: InvocationExpressionSyntax typeMap } })
        {
            convertUsing = expression;
            createMap = typeMap;
            return typeMap.Expression is GenericNameSyntax
                && (typeMap.ArgumentList.Arguments is [] || (typeMap.ArgumentList.Arguments is [{ } typeMapList] && IsMemberList(typeMapList, model)));
        }

        // The chain is read from the outside in: options seen before ReverseMap (outermost first) configure the reverse map.
        if (ReverseMapCall(expression, model) is { } reverseMap)
        {
            // A value converter has no reverse that is sure to be right: only plain options (and Condition/ForPath, TAAK-264) with ReverseMap.
            if (!TryParseOptions(ref expression, model, options, resolvers, converters, reverseExtra) || expression != reverseMap || converters.Count > 0)
            {
                extra.Unsupported = reverseExtra.Unsupported;
                return false;
            }

            reverse = options;
            options = [];
            expression = (InvocationExpressionSyntax)((MemberAccessExpressionSyntax)reverseMap.Expression).Expression;
        }

        if (!TryParseOptions(ref expression, model, options, resolvers, converters, extra) || (reverse is not null && converters.Count > 0))
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
    // resolvers: the members a resolver form of MapFrom configures (see ValueResolvers), with that call.
    // converters: ForMember options with o.ConvertUsing(...) (TAAK-253).
    private static bool TryParseOptions(ref InvocationExpressionSyntax expression, SemanticModel model, Dictionary<string, SimpleLambdaExpressionSyntax?> options,
        Dictionary<string, InvocationExpressionSyntax> resolvers, Dictionary<string, InvocationExpressionSyntax> converters, MemberOptions extra)
    {
        // Options without effect on the hand-written method (TAAK-257) are skipped; MaxDepth and PreserveReferences are checked in Rewrite.
        while (expression is { Expression: MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax skipped } access } forMember
            && (access.Name.Identifier.ValueText is "ForMember" or "ForPath" or "ForCtorParam" || UnsupportedOptions.IsDoNotValidate(forMember, model) || UnsupportedOptions.IsDepthOption(forMember, model)
                || IsConstructUsing(forMember, model)))
        {
            if (IsConstructUsing(forMember, model))
            {
                // Read in Problem (TAAK-266); a second one in the chain is no plain map.
                if (extra.ConstructUsing is not null)
                {
                    return false;
                }

                extra.ConstructUsing = forMember;
                expression = skipped;
                continue;
            }

            if (access.Name.Identifier.ValueText == "ForCtorParam")
            {
                // Read in ConstructorProblem (TAAK-270); any other form of it is no plain map.
                if (!extra.TryParseCtorParam(forMember, model))
                {
                    return false;
                }

                expression = skipped;
                continue;
            }

            if (access.Name.Identifier.ValueText is not ("ForMember" or "ForPath"))
            {
                expression = skipped;
                continue;
            }

            if (Converters.MemberOption(forMember) is var (convertedMember, option)
                && model.GetSymbolInfo(forMember).Symbol is IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly }
                && access.Expression is InvocationExpressionSyntax beforeConverter && !options.ContainsKey(convertedMember) && !resolvers.ContainsKey(convertedMember)
                && converters.TryAdd(convertedMember, option))
            {
                expression = beforeConverter;
                continue;
            }

            if (ValueResolvers.Option(forMember) is var (resolved, call))
            {
                if (model.GetSymbolInfo(forMember).Symbol is not IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly }
                    || access.Expression is not InvocationExpressionSyntax resolvedInner || options.ContainsKey(resolved) || converters.ContainsKey(resolved) || !resolvers.TryAdd(resolved, call))
                {
                    return false;
                }

                expression = resolvedInner;
                continue;
            }

            // Condition, PreCondition and ForPath (TAAK-255).
            if (access.Name.Identifier.ValueText == "ForPath" || ForMemberOption(forMember) is null)
            {
                if (model.GetSymbolInfo(forMember).Symbol is not IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly }
                    || access.Expression is not InvocationExpressionSyntax other || !extra.TryParse(forMember, model, options))
                {
                    return false;
                }

                expression = other;
                continue;
            }

            if (ForMemberOption(forMember) is not var (member, mapFrom) || resolvers.ContainsKey(member) || converters.ContainsKey(member)
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

    private static bool IsConstructUsing(InvocationExpressionSyntax call, SemanticModel model) =>
        call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConstructUsing" }
        && model.GetSymbolInfo(call) is var info && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly };

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
    // A keyword (a ConvertUsing to int, string, ...) gets the .NET name: ToInt32, ToString.
    internal static string MethodName(TypeSyntax destination) =>
        "To" + destination switch
        {
            GenericNameSyntax generic => generic.Identifier.ValueText,
            PredefinedTypeSyntax predefined => predefined.Keyword.ValueText switch
            {
                "bool" => "Boolean", "byte" => "Byte", "sbyte" => "SByte", "short" => "Int16", "ushort" => "UInt16", "int" => "Int32", "uint" => "UInt32",
                "long" => "Int64", "ulong" => "UInt64", "float" => "Single", "double" => "Double", "decimal" => "Decimal", "char" => "Char",
                "string" => "String", _ => "Object",
            },
            _ => destination.ToString().Split('.')[^1],
        };

    // The static class goes right after the profile (the outermost type around a nested one), with its indentation and line endings.
    private static MemberDeclarationSyntax Extensions(TypeDeclarationSyntax anchor, string className, string access,
        List<(TypeSyntax Source, TypeSyntax Destination, List<(string Name, string Value)> Members, bool MapInto, string Creation)> maps,
        List<(TypeSyntax Source, TypeSyntax Destination, string Name, List<(string Name, string Value)> Members, List<CapturedLocal> Parameters)> projections,
        List<CapturedLocal> captured)
    {
        string indent = anchor.GetLeadingTrivia().LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString();
        string eol = anchor.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } found
            ? found
            : "\n";
        var text = new StringBuilder();
        text.Append(access).Append(" static class ").Append(className).Append(eol).Append(indent).Append('{').Append(eol);
        foreach ((TypeSyntax source, TypeSyntax destination, List<(string Name, string Value)> members, _, string creation) in maps)
        {
            if (text[^(eol.Length + 1)] != '{')
            {
                text.Append(eol);
            }

            if (creation.StartsWith(BlockMarker))
            {
                // ConstructUsing with members left to assign (TAAK-266).
                text.Append(indent).Append("    ").Append(access).Append(" static ").Append(destination).Append(' ').Append(MethodName(destination))
                    .Append("(this ").Append(source).Append(" source)").Append(eol).Append(indent).Append("    {").Append(eol)
                    .Append(indent).Append("        ").Append(destination).Append(" destination = ").Append(creation[1..].Replace("\n", "\n" + indent + "        ", StringComparison.Ordinal)).Append(';').Append(eol);
                foreach ((string member, string value) in members)
                {
                    text.Append(indent).Append("        destination.").Append(member).Append(" = ").Append(value).Append(';').Append(eol);
                }

                text.Append(indent).Append("        return destination;").Append(eol).Append(indent).Append("    }").Append(eol);
                continue;
            }

            text.Append(indent).Append("    ").Append(access).Append(" static ").Append(destination).Append(' ').Append(MethodName(destination))
                .Append("(this ").Append(source).Append(" source").Append(CapturedLocal.Declare(CapturedLocal.Used(captured, members))).Append(") => ").Append(creation.Replace("\n", "\n" + indent + "    ", StringComparison.Ordinal));
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
        foreach ((TypeSyntax source, TypeSyntax destination, List<(string Name, string Value)> members, _, _) in maps.Where(m => m.MapInto))
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

        // Properties, not methods: MapCallPattern counts only the ToB methods.
        // A method when the map reads a captured variable of the profile constructor (ProjectTo's parameters), else a property.
        foreach ((TypeSyntax source, TypeSyntax destination, string name, List<(string Name, string Value)> members, List<CapturedLocal> parameters) in projections)
        {
            text.Append(eol).Append(indent).Append("    ").Append(access).Append(" static Expression<Func<").Append(source).Append(", ").Append(destination)
                .Append(">> ").Append(name).Append(parameters.Count == 0 ? "" : "(" + CapturedLocal.Declare(parameters)[2..] + ")").Append(" => source => new ").Append(destination);
            if (members.Count == 0)
            {
                text.Append("();").Append(eol);
                continue;
            }

            text.Append(eol).Append(indent).Append("    {").Append(eol);
            foreach ((string member, string value) in members)
            {
                text.Append(indent).Append("        ").Append(member).Append(" = ").Append(value).Append(',').Append(eol);
            }

            text.Append(indent).Append("    };").Append(eol);
        }

        text.Append(indent).Append('}').Append(eol);
        // The blank line before it is set as trivia: the parser would drop a leading line ending.
        return SyntaxFactory.ParseMemberDeclaration(text.ToString())!.WithLeadingTrivia(SyntaxFactory.EndOfLine(eol), SyntaxFactory.Whitespace(indent));
    }
}
