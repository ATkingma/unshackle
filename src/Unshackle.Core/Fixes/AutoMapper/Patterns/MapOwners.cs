using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// The solution-wide plan of a partial migration (TAAK-272): which CreateMap type pairs stay on AutoMapper 14, with the reason. Every other
/// pair gets one owner, its generated ToB(). A pair stays when its CreateMap statement cannot be rewritten, when a Map, MapInto or ProjectTo
/// call of it cannot be (or is a mock set-up), when a map that stays may need it at run time (member, element, MapFrom or Include types),
/// and when its own rewrite would call the ToY() of a pair that stays. The set only grows until it is stable (a fixpoint), so every
/// pattern, whichever file it fixes, reaches the same decision. Measured (probe of TAAK-272, AutoMapper 14.0.0): a trimmed profile behaves
/// as before for the maps it keeps and passes AssertConfigurationIsValid, as long as the maps those reach stay too.
/// A pair stubbed by a mock set-up of IMapper whose test hands the mock to a class it constructs (the class under test) is shared
/// (TAAK-279): its CreateMap stays for that class, which keeps calling IMapper, and its ToB() is generated for every other call site.
/// </summary>
internal class MapOwners
{
    /// <summary>Start of the reason of every Manual finding that is a planned keep: the rest of the solution is migrated anyway.</summary>
    public const string KeptPrefix = "Stays on AutoMapper: ";

    private const string AutoMapperAssembly = "AutoMapper";

    private static readonly string[] s_mockAssemblies = ["Moq", "NSubstitute", "FakeItEasy"];

    // While a plan is built, the patterns it runs see that plan (as it grows) instead of building another one.
    private static readonly AsyncLocal<MapOwners?> s_planning = new();

    private static readonly ConditionalWeakTable<SolutionId, Entry> s_cache = new();

    private readonly Dictionary<string, string> _pairs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _destinations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _shared = new(StringComparer.Ordinal);
    private readonly HashSet<string> _underTest = new(StringComparer.Ordinal);
    private string? _all;

    /// <summary>The plan without a solution: nothing stays.</summary>
    public static MapOwners None { get; } = new();

    /// <summary>Profile rewrites of the plan being built, per profile; cleared whenever the plan grows.</summary>
    internal Dictionary<SyntaxNode, PatternResult> Rewrites { get; } = [];

    public bool IsEmpty => Count == 0;

    private int Count => _pairs.Count + _destinations.Count + _shared.Count + (_all is null ? 0 : 1);

    /// <summary>
    /// The plan of the solution. The Fixer fixes one file at a time against the original solution, so a plan built for a solution that differs
    /// only in the file of model (the one being fixed) is reused: removing migrated maps does not change which pairs stay.
    /// </summary>
    public static async Task<MapOwners> ForAsync(Solution solution, SemanticModel? model, CancellationToken cancellationToken)
    {
        if (s_planning.Value is { } planning)
        {
            return planning;
        }

        Entry entry = s_cache.GetValue(solution.Id, _ => new Entry());
        Task<MapOwners> plan;
        lock (entry)
        {
            if (entry.Solution is null || entry.Plan is null || entry.Plan.IsFaulted || entry.Plan.IsCanceled || !SameApartFrom(solution, entry.Solution, model?.SyntaxTree.FilePath))
            {
                entry.Solution = solution;
                entry.Plan = BuildAsync(solution, cancellationToken);
            }

            plan = entry.Plan;
        }

        return await plan;
    }

    /// <summary>
    /// Manual with the reason when the Map, MapInto or ProjectTo call stays on IMapper (its pair stays, or it is a mock set-up); null when a
    /// call pattern may rewrite it.
    /// </summary>
    public static async Task<PatternResult?> KeptCallAsync(SyntaxNode call, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        MapOwners owners = await ForAsync(solution, model, cancellationToken);
        if (IsMockSetUp(call, model))
        {
            return PatternResult.Manual($"{KeptPrefix}a mock set-up of IMapper in a test stays as it is: the class under test it is handed to keeps calling IMapper for the maps it stubs (other classes use the generated methods once the test constructs that class itself).");
        }

        return (owners.CallReason(call, model) ?? owners.UnderTestReason(call, model)) is string reason
            ? PatternResult.Manual($"{KeptPrefix}this call keeps using IMapper: {reason}")
            : null;
    }

    /// <summary>
    /// Why the CreateMap of a pair that is migrated stays in the profile as well (a class under test keeps calling IMapper for it, TAAK-279),
    /// or null. A pair that stays on AutoMapper altogether answers <see cref="Reason(ITypeSymbol?, ITypeSymbol?)"/> instead.
    /// </summary>
    public string? Shared(ITypeSymbol from, ITypeSymbol to) => Reason(from, to) is null ? Shared(Key(from), Key(to)) : null;

    /// <summary>Why the pair stays on AutoMapper, or null when it is migrated. An unknown type (null, object, a type parameter) only stays with everything.</summary>
    public string? Reason(ITypeSymbol? source, ITypeSymbol? destination) =>
        destination is null || IsUnknown(destination) ? _all
            : source is null || IsUnknown(source) ? _all ?? _destinations.GetValueOrDefault(Key(destination))
            : Reason(Key(source), Key(destination));

    /// <summary>Why the pair (as <see cref="Key"/> strings) stays on AutoMapper, or null when it is migrated.</summary>
    public string? Reason(string source, string destination) =>
        _all ?? _destinations.GetValueOrDefault(destination) ?? _pairs.GetValueOrDefault(source + "|" + destination);

    // Across compilations a type is the same by its fully qualified name; nullable annotations do not count (as in NestedMaps).
    internal static string Key(ITypeSymbol type) => type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // The pair of a CreateMap statement of a profile constructor (the innermost CreateMap<A, B> of its chain) and whether it has ReverseMap; null for
    // a statement without one (a captured variable, a setting).
    internal static (ITypeSymbol From, ITypeSymbol To, bool Reverse)? StatementPair(SyntaxNode statement, SemanticModel model)
    {
        ExpressionSyntax? expression = (statement as ExpressionStatementSyntax)?.Expression ?? (statement as ArrowExpressionClauseSyntax)?.Expression;
        while (expression is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is GenericNameSyntax { Identifier.ValueText: "CreateMap" } or MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "CreateMap" } })
            {
                return model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly, TypeArguments: [{ } from, { } to] }
                    ? (from, to, CreateMapPattern.HasReverseMap(invocation))
                    : null;
            }

            expression = (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
        }

        return null;
    }

    // A Map call of a Moq, NSubstitute or FakeItEasy set-up or check: on a lambda parameter (Setup(m => m.Map<B>(...))), with an argument
    // matcher (It.IsAny<A>(), Arg.Any<A>(), A<T>.Ignored), followed by Returns, or after Received().
    internal static bool IsMockSetUp(SyntaxNode call, SemanticModel model)
    {
        if (call is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        ExpressionSyntax? receiver = (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
        return (receiver is not null && model.GetSymbolInfo(receiver).Symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } })
            || (receiver is InvocationExpressionSyntax received && IsMockApi(model.GetSymbolInfo(received).Symbol))
            || invocation.ArgumentList.Arguments.Any(a => a.Expression.DescendantNodesAndSelf().Any(n => n is ExpressionSyntax e && IsMockApi(model.GetSymbolInfo(e).Symbol)))
            || (invocation.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax returns } && IsMockApi(model.GetSymbolInfo(returns).Symbol));
    }

    private static bool IsMockApi(ISymbol? symbol) => symbol?.ContainingAssembly?.Name is { } name && s_mockAssemblies.Contains(name, StringComparer.Ordinal);

    // The classes the test of a mock set-up hands its mock to: a new C(...) in the same file with the mock (Moq: mock.Object), or a variable
    // assigned from it, as an IMapper argument. Empty when the mock reaches the code another way (a fixture, a base class, DI): then the pair stays.
    internal static List<INamedTypeSymbol> ClassesUnderTest(SyntaxNode call, SemanticModel model)
    {
        if (call is not InvocationExpressionSyntax invocation || MockVariable(invocation, model) is not { } mock)
        {
            return [];
        }

        SyntaxNode root = call.SyntaxTree.GetRoot();
        HashSet<ISymbol> mocks = new(SymbolEqualityComparer.Default) { mock };
        int count;
        do
        {
            count = mocks.Count;
            foreach (SyntaxNode node in root.DescendantNodes())
            {
                ISymbol? alias = node switch
                {
                    AssignmentExpressionSyntax assignment when IsMock(assignment.Right) => model.GetSymbolInfo(assignment.Left).Symbol,
                    VariableDeclaratorSyntax { Initializer.Value: { } value } declarator when IsMock(value) => model.GetDeclaredSymbol(declarator),
                    _ => null,
                };
                if (alias is ILocalSymbol or IFieldSymbol or IPropertySymbol)
                {
                    mocks.Add(alias);
                }
            }
        }
        while (mocks.Count != count);

        return [.. root.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>()
            .Where(c => c.ArgumentList?.Arguments.Any(a => IsMock(a.Expression) && IsMapper(model.GetTypeInfo(a.Expression).Type)) == true)
            .Select(c => model.GetTypeInfo(c).Type).OfType<INamedTypeSymbol>().Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)];

        bool IsMock(ExpressionSyntax expression) =>
            model.GetSymbolInfo(expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Object" } moq ? moq.Expression : expression).Symbol is { } symbol
            && mocks.Contains(symbol);
    }

    // The variable (local, field, property) that holds the mock of a set-up: m => m.Map(...) inside mock.Setup(...), mock.Received().Map(...),
    // or mock.Map(...) itself (NSubstitute, FakeItEasy).
    private static ISymbol? MockVariable(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        ExpressionSyntax? receiver = (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
        if (receiver is not null && model.GetSymbolInfo(receiver).Symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.AnonymousFunction } })
        {
            receiver = invocation.Ancestors().OfType<LambdaExpressionSyntax>().FirstOrDefault()?.Parent is ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax setUp } }
                ? setUp.Expression
                : null;
        }
        else if (receiver is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax received } && IsMockApi(model.GetSymbolInfo(receiver).Symbol))
        {
            receiver = received.Expression;
        }

        return receiver is not null && model.GetSymbolInfo(receiver).Symbol is { } symbol and (ILocalSymbol or IFieldSymbol or IPropertySymbol) ? symbol : null;
    }

    private static bool IsMapper(ITypeSymbol? type) =>
        type is not null && type.AllInterfaces.Append(type).Any(t => t is { Name: "IMapper", ContainingAssembly.Name: AutoMapperAssembly });

    // A type and its base types, by Key of the definition: a call in a base type runs on the instance of the class under test.
    private static IEnumerable<string> WithBases(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            yield return Key(current.OriginalDefinition);
        }
    }

    private static bool IsUnknown(ITypeSymbol type) =>
        type.TypeKind is TypeKind.Error or TypeKind.Dynamic or TypeKind.TypeParameter || type.SpecialType == SpecialType.System_Object;

    // The source and destination of a Map, MapInto or ProjectTo call (or a Map method group); a collection pair also gives its element pair.
    private static List<(ITypeSymbol? Source, ITypeSymbol? Destination)> CallPairs(SyntaxNode call, SemanticModel model)
    {
        IMethodSymbol? method = model.GetOperation(call) is IMethodReferenceOperation reference ? reference.Method
            : model.GetSymbolInfo(call) is var info ? (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) as IMethodSymbol : null;
        if (method is not { TypeArguments.Length: > 0 })
        {
            return [(null, null)];
        }

        ITypeSymbol destination = method.TypeArguments[^1];
        ITypeSymbol? source = method.TypeArguments.Length == 2 ? method.TypeArguments[0]
            : method.Name == "ProjectTo" ? QueryElement(call, model)
            : call is InvocationExpressionSyntax { ArgumentList.Arguments: [{ } argument, ..] } ? model.GetTypeInfo(argument.Expression).Type
            : model.GetOperation(call) is IMethodReferenceOperation { Parent: IDelegateCreationOperation { Type: INamedTypeSymbol { DelegateInvokeMethod.Parameters: [{ } parameter] } } } ? parameter.Type
            : null;
        List<(ITypeSymbol?, ITypeSymbol?)> pairs = [(source, destination)];
        if (source is not null && MemberValues.TryElement(source, out ITypeSymbol? from, out _) && MemberValues.TryElement(destination, out ITypeSymbol? to, out _))
        {
            pairs.Add((from, to));
        }

        return pairs;
    }

    // The element type of the query of query.ProjectTo<B>() (or mapper.ProjectTo<B>(query)), or null.
    private static ITypeSymbol? QueryElement(SyntaxNode call, SemanticModel model)
    {
        ExpressionSyntax? query = call is InvocationExpressionSyntax invocation && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ReducedFrom: not null }
            ? (invocation.Expression as MemberAccessExpressionSyntax)?.Expression
            : (call as InvocationExpressionSyntax)?.ArgumentList.Arguments.FirstOrDefault()?.Expression;
        return query is not null && model.GetTypeInfo(query).Type is { } type
            && type.AllInterfaces.Append(type).OfType<INamedTypeSymbol>().FirstOrDefault(i => i.Name == "IQueryable" && i.TypeArguments.Length == 1) is { TypeArguments: [{ } element] }
            ? element
            : null;
    }

    private static string Where(SyntaxNode node) =>
        $"{Path.GetFileName(node.SyntaxTree.FilePath)}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";

    private static string Name(ITypeSymbol? type) => type?.Name is { Length: > 0 } name ? name : "?";

    // Whether two solutions are the same apart from the documents of one file (the one being fixed).
    private static bool SameApartFrom(Solution solution, Solution cached, string? filePath)
    {
        if (ReferenceEquals(solution, cached))
        {
            return true;
        }

        SolutionChanges changes = solution.GetChanges(cached);
        if (changes.GetAddedProjects().Any() || changes.GetRemovedProjects().Any())
        {
            return false;
        }

        foreach (ProjectChanges project in changes.GetProjectChanges())
        {
            if (project.GetAddedDocuments().Any() || project.GetRemovedDocuments().Any() || project.GetAddedMetadataReferences().Any()
                || project.GetRemovedMetadataReferences().Any() || project.GetAddedProjectReferences().Any() || project.GetRemovedProjectReferences().Any()
                || project.GetChangedDocuments().Any(id => solution.GetDocument(id)?.FilePath is not { } path || path != filePath))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<MapOwners> BuildAsync(Solution solution, CancellationToken cancellationToken)
    {
        var owners = new MapOwners();
        s_planning.Value = owners;
        var mapCall = new MapCallPattern();
        var mapInto = new MapIntoPattern();
        var projectTo = new ProjectToPattern();
        List<(ClassDeclarationSyntax Profile, SemanticModel Model, List<MapNode> Nodes)> profiles = [];
        List<(SyntaxNode Call, SemanticModel Model, ISolutionPattern? Pattern)> calls = [];
        List<(SyntaxNode Root, SemanticModel Model)> sources = [];
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!(await document.GetTextAsync(cancellationToken)).ToString().Contains("Map", StringComparison.Ordinal)
                || await document.GetSyntaxRootAsync(cancellationToken) is not { } root || await document.GetSemanticModelAsync(cancellationToken) is not { } model)
            {
                continue;
            }

            sources.Add((root, model));
            foreach (ClassDeclarationSyntax profile in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => CreateMapPattern.IsProfileWithMaps(c, model)))
            {
                List<MapNode> nodes = [];
                foreach (SyntaxNode statement in profile.Members.OfType<ConstructorDeclarationSyntax>().SelectMany(CreateMapPattern.MapStatements))
                {
                    if (StatementPair(statement, model) is var (from, to, reverse))
                    {
                        nodes.Add(new MapNode(statement, model, from, to, reverse));
                    }
                }

                profiles.Add((profile, model, nodes));
            }

            calls.AddRange(mapCall.Find(root, model).Select(n => (n, model, (ISolutionPattern?)mapCall)));
            calls.AddRange(mapInto.Find(root, model).Select(n => (n, model, (ISolutionPattern?)mapInto)));
            calls.AddRange(projectTo.Find(root, model).Select(n => (n, model, (ISolutionPattern?)projectTo)));
            calls.AddRange(root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => CreateMapPattern.IsOtherMapCall(i, model)).Select(n => ((SyntaxNode)n, model, (ISolutionPattern?)null)));
        }

        List<MapNode> all = [.. profiles.SelectMany(p => p.Nodes)];
        Dictionary<SyntaxNode, List<INamedTypeSymbol>> stubs = [];
        foreach ((SyntaxNode call, SemanticModel model, _) in calls.Where(c => IsMockSetUp(c.Call, c.Model)))
        {
            stubs[call] = ClassesUnderTest(call, model);
            stubs[call].ForEach(c => owners._underTest.UnionWith(WithBases(c)));
        }

        owners.CloseUnderTest(sources);
        int count;
        do
        {
            count = owners.Count;
            owners.Rewrites.Clear();
            foreach ((ClassDeclarationSyntax profile, SemanticModel model, List<MapNode> nodes) in profiles)
            {
                foreach ((SyntaxNode? statement, string reason) in await CreateMapPattern.PlanAsync(profile, model, solution, cancellationToken))
                {
                    foreach (MapNode node in nodes.Where(n => statement is null || n.Statement == statement))
                    {
                        owners.Keep(node, reason);
                    }
                }
            }

            foreach ((SyntaxNode call, SemanticModel model, ISolutionPattern? pattern) in calls)
            {
                if (stubs.TryGetValue(call, out List<INamedTypeSymbol>? underTest) && underTest.Count > 0)
                {
                    owners.ShareCall(call, model, $"{string.Join(", ", underTest.Select(c => c.Name))} keeps calling IMapper for it, because its test stubs this map ({Where(call)}); every other call site uses the generated method");
                    continue;
                }

                if (pattern is not null && owners.ClassUnderTest(call, model) is { } owner)
                {
                    owners.ShareCall(call, model, $"{owner.Name} keeps calling IMapper for it (a test hands it a mocked IMapper, {Where(call)}); every other call site uses the generated method");
                    continue;
                }

                string? reason = IsMockSetUp(call, model) ? "it is stubbed by a mock set-up of IMapper in a test that does not construct its class under test itself"
                    : pattern is null ? "a ProjectTo or Map with options at " + Where(call) + " is not migrated (automapper.unsupported)"
                    : await pattern.RewriteAsync(call, model, solution, cancellationToken) is ManualResult { Reason: var manual } && !manual.StartsWith(KeptPrefix, StringComparison.Ordinal)
                        ? $"the call at {Where(call)} is not migrated: {manual}"
                        : null;
                if (reason is not null)
                {
                    owners.KeepCall(call, model, reason);
                }
            }

            owners.Close(all);
        }
        while (owners.Count != count);

        owners.Rewrites.Clear();
        return owners;
    }

    // Why this call stays: the reason of its pair (or element pair), or null.
    private string? CallReason(SyntaxNode call, SemanticModel model) =>
        CallPairs(call, model).Select(p => Reason(p.Source, p.Destination) is string reason ? $"the map {Name(p.Source)} → {Name(p.Destination)} stays on AutoMapper ({reason})" : null)
            .FirstOrDefault(r => r is not null);

    // Why a call stays: it sits in a class under test (or a type that class hands its IMapper to). Its test runs it on the mock, stubbed or
    // not (a loose mock returns null for a map it does not stub), so every Map call there keeps IMapper and its pair stays shared.
    private string? UnderTestReason(SyntaxNode call, SemanticModel model) =>
        ClassUnderTest(call, model) is { } owner
            ? $"{owner.Name} is a class under test (a test hands it a mocked IMapper), so it keeps calling IMapper; other classes call the generated method"
            : null;

    // The innermost type around the call that is a class under test (or a type it hands its IMapper to), or null.
    private INamedTypeSymbol? ClassUnderTest(SyntaxNode call, SemanticModel model) =>
        call.Ancestors().OfType<TypeDeclarationSyntax>().Select(t => model.GetDeclaredSymbol(t) as INamedTypeSymbol)
            .FirstOrDefault(t => t is not null && _underTest.Contains(Key(t.OriginalDefinition)));

    // The classes under test also cover every type they hand an IMapper to (a constructor or method argument), whose calls run the stub too.
    private void CloseUnderTest(List<(SyntaxNode Root, SemanticModel Model)> sources)
    {
        int count;
        do
        {
            count = _underTest.Count;
            foreach ((SyntaxNode root, SemanticModel model) in sources)
            {
                foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                    .Where(t => model.GetDeclaredSymbol(t) is INamedTypeSymbol symbol && _underTest.Contains(Key(symbol.OriginalDefinition))))
                {
                    foreach (SyntaxNode node in type.DescendantNodes())
                    {
                        ArgumentListSyntax? arguments = node switch
                        {
                            InvocationExpressionSyntax invocation => invocation.ArgumentList,
                            BaseObjectCreationExpressionSyntax creation => creation.ArgumentList,
                            ConstructorInitializerSyntax initializer => initializer.ArgumentList,
                            _ => null,
                        };
                        if (arguments is not null && arguments.Arguments.Any(a => IsMapper(model.GetTypeInfo(a.Expression).Type))
                            && model.GetSymbolInfo(node).Symbol is IMethodSymbol { ContainingType: { } target } && target.Locations.Any(l => l.IsInSource))
                        {
                            _underTest.UnionWith(WithBases(target));
                        }
                    }
                }
            }
        }
        while (_underTest.Count != count);
    }

    // A pair stubbed for a class under test: shared, unless its types are only known at run time (then it stays like any other kept call).
    private void ShareCall(SyntaxNode call, SemanticModel model, string reason)
    {
        foreach ((ITypeSymbol? source, ITypeSymbol? destination) in CallPairs(call, model))
        {
            if (source is null || destination is null || IsUnknown(source) || IsUnknown(destination))
            {
                KeepCall(call, model, "it is stubbed by a mock set-up of IMapper in a test, for types only known at run time");
                return;
            }

            _shared.TryAdd(Key(source) + "|" + Key(destination), reason);
        }
    }

    private string? Shared(string source, string destination) => _shared.GetValueOrDefault(source + "|" + destination);

    private void Share(MapNode node, string reason)
    {
        _shared.TryAdd(node.FromKey + "|" + node.ToKey, reason);
        if (node.Reverse)
        {
            _shared.TryAdd(node.ToKey + "|" + node.FromKey, reason);
        }
    }

    private void KeepCall(SyntaxNode call, SemanticModel model, string reason)
    {
        foreach ((ITypeSymbol? source, ITypeSymbol? destination) in CallPairs(call, model))
        {
            if (destination is null || IsUnknown(destination))
            {
                _all ??= $"{reason} (its destination is only known at run time, so every map stays)";
            }
            else if (source is null || IsUnknown(source))
            {
                _destinations.TryAdd(Key(destination), $"{reason} (its source is only known at run time, so every map to {destination.Name} stays)");
            }
            else
            {
                _pairs.TryAdd(Key(source) + "|" + Key(destination), reason);
            }
        }
    }

    private void Keep(MapNode node, string reason)
    {
        _pairs.TryAdd(node.FromKey + "|" + node.ToKey, reason);
        if (node.Reverse)
        {
            _pairs.TryAdd(node.ToKey + "|" + node.FromKey, reason);
        }
    }

    // A statement stays with both its maps; a map that stays keeps every map AutoMapper may run for it.
    private void Close(List<MapNode> nodes)
    {
        bool grew = true;
        while (grew)
        {
            grew = false;
            foreach (MapNode node in nodes)
            {
                string? reason = Reason(node.FromKey, node.ToKey) ?? (node.Reverse ? Reason(node.ToKey, node.FromKey) : null);
                if (reason is null)
                {
                    // A shared map stays in the profile too, and with it every map AutoMapper may run for it (shared as well, unless kept).
                    if ((Shared(node.FromKey, node.ToKey) ?? (node.Reverse ? Shared(node.ToKey, node.FromKey) : null)) is string shared)
                    {
                        int was = Count;
                        Share(node, shared);
                        foreach (MapNode needed in nodes.Where(n => n != node && node.Reaches(n) && Reason(n.FromKey, n.ToKey) is null))
                        {
                            Share(needed, $"CreateMap<{node.From.Name}, {node.To.Name}> stays for a class under test and may map its members through this map");
                        }

                        grew |= Count != was;
                    }

                    continue;
                }

                int before = Count;
                Keep(node, reason);
                foreach (MapNode needed in nodes.Where(n => n != node && node.Reaches(n)))
                {
                    if (Reason(needed.FromKey, needed.ToKey) is null)
                    {
                        Keep(needed, $"CreateMap<{node.From.Name}, {node.To.Name}> stays on AutoMapper and may map its members through this map");
                    }
                }

                grew |= Count != before;
            }
        }
    }

    private class Entry
    {
        public Solution? Solution { get; set; }

        public Task<MapOwners>? Plan { get; set; }
    }

    // One CreateMap statement of a profile constructor, with the types AutoMapper may meet while it runs that map.
    private class MapNode
    {
        private readonly HashSet<string> _reached = new(StringComparer.Ordinal);

        public MapNode(SyntaxNode statement, SemanticModel model, ITypeSymbol from, ITypeSymbol to, bool reverse)
        {
            Statement = statement;
            From = from;
            To = to;
            Reverse = reverse;
            FromKey = Key(from);
            ToKey = Key(to);
            // Over-approximated (safe: more maps stay): member and constructor parameter types of both sides, their elements, and every type the
            // statement names or computes (MapFrom results, Include<C, D>).
            foreach (ITypeSymbol type in new[] { from, to })
            {
                Add(type);
                foreach (ISymbol member in CreateMapPattern.AllMembers(type))
                {
                    Add(member switch { IPropertySymbol p => p.Type, IFieldSymbol f => f.Type, _ => null });
                }

                foreach (IMethodSymbol constructor in (type as INamedTypeSymbol)?.InstanceConstructors ?? [])
                {
                    constructor.Parameters.ToList().ForEach(p => Add(p.Type));
                }
            }

            foreach (SyntaxNode node in statement.DescendantNodes())
            {
                Add(node is ExpressionSyntax or TypeSyntax ? model.GetTypeInfo(node).Type : null);
            }
        }

        public SyntaxNode Statement { get; }

        public ITypeSymbol From { get; }

        public ITypeSymbol To { get; }

        public bool Reverse { get; }

        public string FromKey { get; }

        public string ToKey { get; }

        public bool Reaches(MapNode other) => _reached.Contains(other.FromKey) && _reached.Contains(other.ToKey);

        private void Add(ITypeSymbol? type)
        {
            if (type is null || type.TypeKind == TypeKind.Error || !_reached.Add(Key(type)))
            {
                return;
            }

            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T, TypeArguments: [{ } value] })
            {
                Add(value);
            }

            if (MemberValues.TryElement(type, out ITypeSymbol? element, out _))
            {
                Add(element);
            }
        }
    }
}
