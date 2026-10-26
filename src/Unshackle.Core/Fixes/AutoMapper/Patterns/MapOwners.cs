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
/// TAAK-311 (council A1): a mock whose stubs provably return what the generated methods return (<see cref="MockedMappers"/>), and that
/// serves only maps that are all migrated, does not keep its classes on IMapper: its stubs go (with a warning) and the classes migrate.
/// When that turns out not to hold once the plan is stable, the mock is vetoed and the plan is built again without it.
/// </summary>
internal class MapOwners
{
    private const string AutoMapperAssembly = "AutoMapper";

    private static readonly string[] s_mockAssemblies = ["Moq", "NSubstitute", "FakeItEasy"];

    // While a plan is built, the patterns it runs see that plan (as it grows) instead of building another one.
    private static readonly AsyncLocal<MapOwners?> s_planning = new();

    private static readonly ConditionalWeakTable<SolutionId, MapOwnerEntry> s_cache = new();

    private readonly Dictionary<string, string> _pairs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _destinations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _shared = new(StringComparer.Ordinal);
    private readonly HashSet<string> _underTest = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _underTestWhere = new(StringComparer.Ordinal);
    private readonly HashSet<string> _discharged = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dischargedCalls = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MockReason> _stubReasons = new(StringComparer.Ordinal);
    private readonly HashSet<string> _checks = new(StringComparer.Ordinal);
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

        MapOwnerEntry entry = s_cache.GetValue(solution.Id, _ => new MapOwnerEntry());
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
        return owners.KeptCall(call, model);
    }

    /// <summary><see cref="KeptCallAsync"/> on this plan: Manual with the reason when the call stays on IMapper, or null.</summary>
    public PatternResult? KeptCall(SyntaxNode call, SemanticModel model)
    {
        if (IsMockSetUp(call, model))
        {
            string key = CallKey(call);
            if (_checks.Contains(key))
            {
                // Council A6: Verify / Received of a Map call stays; no Received() equivalent remains for the generated method.
                return new ManualResult($"the check of IMapper.Map at {Where(call)} stays as it is: once the class under test calls the generated method there is no "
                    + "Received() equivalent left to verify, so its class keeps calling IMapper; assert the mapped result instead, then run the fix again.")
                {
                    Kept = KeptReason.MockSetUpKept,
                    Code = nameof(MockSetUpCode.VerifyOnMapper),
                };
            }

            if (_dischargedCalls.Contains(key))
            {
                return null;
            }

            MockReason? why = _stubReasons.GetValueOrDefault(key);
            return new ManualResult("a mock set-up of IMapper in a test stays as it is: the class under test it is handed to keeps calling IMapper for the maps it stubs (other classes use the generated methods once the test constructs that class itself)."
                + (why is not null ? $" The stub at {Where(call)} is not removed: {why}." : ""))
            {
                Kept = KeptReason.MockSetUpKept,
                Code = why?.Code?.ToString(),
            };
        }

        return (CallReason(call, model) ?? UnderTestReason(call, model)) is string reason
            ? PatternResult.KeptManual(KeptReason.CallKept, $"this call keeps using IMapper: {reason}")
            : null;
    }

    /// <summary>
    /// Why the CreateMap of a pair that is migrated stays in the profile as well (a class under test keeps calling IMapper for it, TAAK-279),
    /// or null. A pair that stays on AutoMapper altogether answers <see cref="Reason(ITypeSymbol?, ITypeSymbol?)"/> instead.
    /// </summary>
    public string? Shared(ITypeSymbol from, ITypeSymbol to) => Reason(from, to) is null ? Shared(Key(from), Key(to)) : null;

    /// <summary>Whether the stubs of this mock (<see cref="MockedMapper.Key"/>) go and its classes migrate (TAAK-311).</summary>
    public bool IsDischarged(string mockKey) => _discharged.Contains(mockKey);

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
        ExpressionSyntax? expression = (statement as ExpressionStatementSyntax)?.Expression ?? (statement as ArrowExpressionClauseSyntax)?.Expression ?? statement as ExpressionSyntax;
        while (expression is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is GenericNameSyntax { Identifier.ValueText: "CreateMap" } or MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "CreateMap" } })
            {
                return model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly, TypeArguments: [{ } from, { } to] }
                    ? (from, to, ProfileSyntax.HasReverseMap(invocation))
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

    internal static bool IsMockApi(ISymbol? symbol) => symbol?.ContainingAssembly?.Name is { } name && s_mockAssemblies.Contains(name, StringComparer.Ordinal);

    // The classes the test of a mock set-up hands its mock to: a new C(...) in the same file with the mock (Moq: mock.Object), or a variable
    // assigned from it, as an IMapper argument. Empty when the mock reaches the code another way (a fixture, a base class, DI): then the pair stays.
    internal static List<INamedTypeSymbol> ClassesUnderTest(SyntaxNode call, SemanticModel model) =>
        call is InvocationExpressionSyntax invocation && MockVariable(invocation, model) is { } mock
            ? Constructed(call.SyntaxTree.GetRoot(), model, new HashSet<ISymbol>(SymbolEqualityComparer.Default) { mock })
            : [];

    // Every class a file constructs with a mocked IMapper (new Mock<IMapper>(), Substitute.For<IMapper>(), A.Fake<IMapper>(), or a variable
    // holding one), stubbed or not: a loose mock answers a Map it does not stub with null, so those calls must stay on IMapper too.
    internal static List<INamedTypeSymbol> MockedClasses(SyntaxNode root, SemanticModel model) =>
        Constructed(root, model, new HashSet<ISymbol>(SymbolEqualityComparer.Default));

    // The types of new C(...) in the file with a mock (one of mocks, a variable assigned from one, or a mock created in place) as an IMapper argument.
    private static List<INamedTypeSymbol> Constructed(SyntaxNode root, SemanticModel model, HashSet<ISymbol> mocks)
    {
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

        bool IsMock(ExpressionSyntax expression)
        {
            ExpressionSyntax mock = expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Object" } moq ? moq.Expression : expression;
            return (model.GetSymbolInfo(mock).Symbol is { } symbol && mocks.Contains(symbol))
                || (mock is BaseObjectCreationExpressionSyntax or InvocationExpressionSyntax && IsMockApi(model.GetSymbolInfo(mock).Symbol)
                    && model.GetTypeInfo(mock).Type is { } type && (IsMapper(type) || type is INamedTypeSymbol { Name: "Mock", TypeArguments: [{ } mocked] } && IsMapper(mocked)));
        }
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

    // The type a declaration declares; null for a C# 14 extension block, for which GetDeclaredSymbol throws (measured on kavita).
    private static INamedTypeSymbol? TypeOf(TypeDeclarationSyntax declaration, SemanticModel model) =>
        declaration is ExtensionBlockDeclarationSyntax ? null : model.GetDeclaredSymbol(declaration) as INamedTypeSymbol;

    internal static bool IsMapper(ITypeSymbol? type) =>
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

    internal static string Where(SyntaxNode node) =>
        $"{Path.GetFileName(node.SyntaxTree.FilePath)}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";

    private static string Name(ITypeSymbol? type) => type?.Name is { Length: > 0 } name ? name : "?";

    // Whether two solutions are the same apart from the documents of one file (the one being fixed).
    private static bool SameApartFrom(Solution solution, Solution cached, string? filePath) => SameApartFrom(solution, cached, [filePath]);

    // Whether two solutions are the same apart from the documents of the given files (MapperCleanup: the file its plan was built
    // from and the one being fixed).
    internal static bool SameApartFrom(Solution solution, Solution cached, IReadOnlyCollection<string?> filePaths)
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
                || project.GetChangedDocuments().Any(id => solution.GetDocument(id)?.FilePath is not { } path || !filePaths.Contains(path)))
            {
                return false;
            }
        }

        return true;
    }

    // The key of a Map call across semantic models of its file (the plan's model and the one a pattern fixes).
    private static string CallKey(SyntaxNode call) =>
        $"{call.SyntaxTree.FilePath}|{call.GetLocation().GetLineSpan().StartLinePosition.Line}|{call}";

    // TAAK-311: a mock that turns out not to qualify once the plan is stable is vetoed (with the reason) and the plan is built again.
    private static async Task<MapOwners> BuildAsync(Solution solution, CancellationToken cancellationToken)
    {
        Dictionary<string, MockReason> vetoed = new(StringComparer.Ordinal);
        while (true)
        {
            (MapOwners owners, Dictionary<string, MockReason> failures) = await BuildOnceAsync(solution, vetoed, cancellationToken);
            if (failures.Count == 0)
            {
                return owners;
            }

            foreach ((string key, MockReason reason) in failures)
            {
                vetoed.TryAdd(key, reason);
            }
        }
    }

    private static async Task<(MapOwners Owners, Dictionary<string, MockReason> Failures)> BuildOnceAsync(Solution solution, IReadOnlyDictionary<string, MockReason> vetoed,
        CancellationToken cancellationToken)
    {
        var owners = new MapOwners();
        s_planning.Value = owners;
        var mapCall = new MapCallPattern();
        var genericMap = new GenericMapPattern();
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
            foreach (ClassDeclarationSyntax profile in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => ProfileSyntax.IsProfileWithMaps(c, model)))
            {
                List<MapNode> nodes = [];
                foreach (SyntaxNode statement in ProfileSyntax.MapStatements(profile, model))
                {
                    if (StatementPair(statement, model) is var (from, to, reverse))
                    {
                        nodes.Add(new MapNode(statement, model, from, to, reverse));
                    }
                }

                profiles.Add((profile, model, nodes));
            }

            calls.AddRange(mapCall.Find(root, model).Select(n => (n, model, (ISolutionPattern?)mapCall)));
            calls.AddRange(genericMap.Find(root, model).Select(n => (n, model, (ISolutionPattern?)genericMap)));
            calls.AddRange(mapInto.Find(root, model).Select(n => (n, model, (ISolutionPattern?)mapInto)));
            calls.AddRange(projectTo.Find(root, model).Select(n => (n, model, (ISolutionPattern?)projectTo)));
            calls.AddRange(root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => CreateMapPattern.IsOtherMapCall(i, model)).Select(n => ((SyntaxNode)n, model, (ISolutionPattern?)null)));
        }

        List<MapNode> all = [.. profiles.SelectMany(p => p.Nodes)];
        HashSet<string> plain = PlainPairs(profiles);
        Dictionary<string, MockedMapper> candidates = new(StringComparer.Ordinal);
        HashSet<SyntaxNode> dischargedCalls = [];
        Dictionary<SyntaxNode, List<INamedTypeSymbol>> stubs = [];
        foreach ((SyntaxNode root, SemanticModel model) in sources.Where(s => s.Root.ToString().Contains("IMapper", StringComparison.Ordinal)))
        {
            HashSet<ISymbol> candidateMocks = new(SymbolEqualityComparer.Default);
            Dictionary<ISymbol, string> reasons = new(SymbolEqualityComparer.Default);
            foreach (MockedMapper mock in MockedMappers.Find(root, model))
            {
                owners._checks.UnionWith(mock.Checks.Select(c => CallKey(c)));

                // A mock without stubs is a candidate too (council A8): Undischarged checks that its classes never read what Map returns.
                MockReason? why = mock.Problem ?? vetoed.GetValueOrDefault(mock.Key)
                    ?? (mock.Classes.Count == 0 ? MockReason.Of("the test does not construct its class under test with this mock") : null)
                    ?? mock.Stubs.Select(s => MockedMappers.Disproof(s, model, (a, b) => plain.Contains(Key(a) + "|" + Key(b)))).FirstOrDefault(r => r is not null);
                if (why is null)
                {
                    candidates[mock.Key] = mock;
                    candidateMocks.Add(mock.Mock);
                    dischargedCalls.UnionWith(mock.Stubs.Select(s => s.Call));
                    continue;
                }

                reasons[mock.Mock] = $"{mock.Where}: {why}";
                foreach (MapStub stub in mock.Stubs)
                {
                    owners._stubReasons.TryAdd(CallKey(stub.Call), why);
                }
            }

            foreach (INamedTypeSymbol type in MockedClasses(root, model))
            {
                if (UnderTestBy(type, root, model, candidateMocks, reasons) is string where)
                {
                    foreach (string key in WithBases(type))
                    {
                        owners._underTest.Add(key);
                        owners._underTestWhere.TryAdd(key, where);
                    }
                }
            }
        }

        foreach ((SyntaxNode call, SemanticModel model, _) in calls.Where(c => IsMockSetUp(c.Call, c.Model) && !dischargedCalls.Contains(c.Call)))
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
                if (dischargedCalls.Contains(call))
                {
                    continue;
                }

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
                    : await pattern.RewriteAsync(call, model, solution, cancellationToken) is ManualResult { Kept: KeptReason.None, Reason: var manual }
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
        Dictionary<string, MockReason> failures = new(StringComparer.Ordinal);
        foreach ((string key, MockedMapper mock) in candidates)
        {
            if (owners.Undischarged(mock, calls, sources) is { } failure)
            {
                failures[key] = failure;
                continue;
            }

            owners._discharged.Add(key);
            owners._dischargedCalls.UnionWith(mock.Stubs.Select(s => CallKey(s.Call)));
        }

        return (owners, failures);
    }

    // The pairs (Key|Key) of plain CreateMap<A, B>() statements (ReverseMap() at most) of profiles whose one constructor holds only CreateMap
    // statements (options on another map do not touch this one; a profile setting would); a pair that is also mapped another way is not plain.
    private static HashSet<string> PlainPairs(List<(ClassDeclarationSyntax Profile, SemanticModel Model, List<MapNode> Nodes)> profiles)
    {
        HashSet<string> plain = new(StringComparer.Ordinal);
        HashSet<string> other = new(StringComparer.Ordinal);
        foreach ((ClassDeclarationSyntax profile, SemanticModel model, List<MapNode> nodes) in profiles)
        {
            bool onlyMaps = profile.Members.OfType<ConstructorDeclarationSyntax>().ToList() is [{ Body: { } body, ExpressionBody: null }]
                && body.Statements.All(s => StatementPair(s, model) is not null);
            foreach (MapNode node in nodes)
            {
                HashSet<string> target = onlyMaps && node.Statement is StatementSyntax statement && IsPlainCreateMap(statement) ? plain : other;
                target.Add(node.FromKey + "|" + node.ToKey);
                if (node.Reverse)
                {
                    target.Add(node.ToKey + "|" + node.FromKey);
                }
            }
        }

        plain.ExceptWith(other);
        return plain;
    }

    private static bool IsPlainCreateMap(StatementSyntax statement)
    {
        if (statement is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation })
        {
            return false;
        }

        if (invocation is { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ReverseMap", Expression: InvocationExpressionSyntax inner }, ArgumentList.Arguments.Count: 0 })
        {
            invocation = inner;
        }

        return invocation.ArgumentList.Arguments.Count == 0
            && invocation.Expression is GenericNameSyntax { Identifier.ValueText: "CreateMap", TypeArgumentList.Arguments.Count: 2 }
                or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: GenericNameSyntax { Identifier.ValueText: "CreateMap", TypeArgumentList.Arguments.Count: 2 } };
    }

    // Where the file puts the class under test (file:line and why), or null when only mocks whose stubs go construct it.
    private static string? UnderTestBy(INamedTypeSymbol type, SyntaxNode root, SemanticModel model, HashSet<ISymbol> candidates, Dictionary<ISymbol, string> reasons)
    {
        foreach (BaseObjectCreationExpressionSyntax creation in root.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>()
            .Where(c => SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(c).Type, type)))
        {
            foreach (ArgumentSyntax argument in creation.ArgumentList?.Arguments ?? [])
            {
                if (!IsMapper(model.GetTypeInfo(argument.Expression).Type))
                {
                    continue;
                }

                ExpressionSyntax held = argument.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Object" } moq ? moq.Expression : argument.Expression;
                ISymbol? mock = model.GetSymbolInfo(held).Symbol;
                if (mock is null || !candidates.Contains(mock))
                {
                    return mock is not null && reasons.TryGetValue(mock, out string? reason) ? reason : Where(creation);
                }
            }
        }

        return null;
    }

    // Why a candidate mock does not qualify once the plan is stable, or null: one of its classes stays under test, hands its IMapper on,
    // or makes a Map call that is kept, not migrated, or not stubbed by this mock; or a stubbed map stays on AutoMapper.
    private MockReason? Undischarged(MockedMapper mock, List<(SyntaxNode Call, SemanticModel Model, ISolutionPattern? Pattern)> calls, List<(SyntaxNode Root, SemanticModel Model)> sources)
    {
        HashSet<string> classes = [.. mock.Classes.SelectMany(WithBases)];
        if (mock.Classes.FirstOrDefault(c => _underTest.Contains(Key(c.OriginalDefinition))) is { } kept)
        {
            return MockReason.Of($"{kept.Name} stays a class under test of another mock ({_underTestWhere.GetValueOrDefault(Key(kept.OriginalDefinition))})");
        }

        if (Targets(sources, classes).FirstOrDefault(t => !classes.Contains(Key(t.OriginalDefinition))) is { } target)
        {
            return MockReason.Of($"a class it is handed to passes its IMapper on to {target.Name}");
        }

        HashSet<string> stubbed = new(StringComparer.Ordinal);
        foreach (MapStub stub in mock.Stubs)
        {
            if (Reason(stub.From, stub.To) is string reason)
            {
                return MockReason.Of($"the stubbed map {Name(stub.From)} → {stub.To.Name} stays on AutoMapper ({reason})");
            }

            stubbed.Add(Key(stub.From!) + "|" + Key(stub.To));
        }

        foreach ((SyntaxNode call, SemanticModel model, ISolutionPattern? pattern) in calls)
        {
            if (call.Ancestors().OfType<TypeDeclarationSyntax>().Select(t => TypeOf(t, model)).FirstOrDefault(t => t is not null) is not { } owner
                || !classes.Contains(Key(owner.OriginalDefinition)))
            {
                continue;
            }

            if (pattern is null || KeptCall(call, model) is ManualResult)
            {
                return MockReason.Of($"the call at {Where(call)} keeps using IMapper");
            }

            if (mock.Stubs.Count == 0)
            {
                // Council A8: the loose mock answers null, the generated method a real object; only a value nobody reads hides the difference.
                if (ValueUse(call, model, pattern) is string use)
                {
                    return MockReason.Of(MockSetUpCode.MockWithoutStubsNotProvable,
                        $"the mock stubs no map (it answers Map with null, the generated method returns a real object), and the value of the call at {Where(call)} {use}");
                }

                continue;
            }

            if (CallPairs(call, model) is [(ITypeSymbol source, ITypeSymbol destination), ..] && !stubbed.Contains(Key(source) + "|" + Key(destination)))
            {
                return MockReason.Of($"the map {source.Name} → {destination.Name} at {Where(call)} is not stubbed by this mock (the mock answers it with null, the generated method does not)");
            }
        }

        return null;
    }

    // Council A8: how the class under test uses the value of a Map call (read, null-checked, returned, passed or stored, with file:line), or null
    // when nothing reads it: the call is a statement of its own, a discard, or the initializer of a local that is never read. Only a plain Map
    // call qualifies (Map into an existing object fills it, which the mock does not).
    private static string? ValueUse(SyntaxNode call, SemanticModel model, ISolutionPattern pattern)
    {
        if (pattern is not MapCallPattern || call is not InvocationExpressionSyntax invocation)
        {
            return "is not a plain Map call";
        }

        switch (invocation.Parent)
        {
            case ExpressionStatementSyntax:
                return null;
            case AssignmentExpressionSyntax { Left: IdentifierNameSyntax { Identifier.ValueText: "_" } discard, Parent: ExpressionStatementSyntax } when model.GetSymbolInfo(discard).Symbol is IDiscardSymbol or null:
                return null;
            case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator } when model.GetDeclaredSymbol(declarator) is ILocalSymbol local
                && declarator.FirstAncestorOrSelf<MemberDeclarationSyntax>() is { } member:
                return member.DescendantNodes().OfType<IdentifierNameSyntax>()
                    .FirstOrDefault(n => n.Identifier.ValueText == local.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, local)) is { } read
                    ? $"is read at {Where(read)}"
                    : null;
            default:
                return "is used there (returned, passed, stored, checked or read)";
        }
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
                + (_underTestWhere.GetValueOrDefault(Key(owner.OriginalDefinition)) is { } where ? $" (test: {where})" : "")
            : null;

    // The innermost type around the call that is a class under test (or a type it hands its IMapper to), or null.
    private INamedTypeSymbol? ClassUnderTest(SyntaxNode call, SemanticModel model) =>
        call.Ancestors().OfType<TypeDeclarationSyntax>().Select(t => TypeOf(t, model))
            .FirstOrDefault(t => t is not null && _underTest.Contains(Key(t.OriginalDefinition)));

    // The classes under test also cover every type they hand an IMapper to (a constructor or method argument), whose calls run the stub too.
    private void CloseUnderTest(List<(SyntaxNode Root, SemanticModel Model)> sources)
    {
        int count;
        do
        {
            count = _underTest.Count;
            foreach (INamedTypeSymbol target in Targets(sources, _underTest))
            {
                foreach (string key in WithBases(target))
                {
                    _underTest.Add(key);
                    _underTestWhere.TryAdd(key, "a class under test hands it its IMapper");
                }
            }
        }
        while (_underTest.Count != count);
    }

    // The source types the given types (by Key) hand an IMapper to: a method, constructor or constructor initializer with an IMapper argument.
    private static List<INamedTypeSymbol> Targets(List<(SyntaxNode Root, SemanticModel Model)> sources, HashSet<string> types)
    {
        List<INamedTypeSymbol> targets = [];
        foreach ((SyntaxNode root, SemanticModel model) in sources)
        {
            foreach (TypeDeclarationSyntax type in root.DescendantNodes().OfType<TypeDeclarationSyntax>()
                .Where(t => TypeOf(t, model) is { } symbol && types.Contains(Key(symbol.OriginalDefinition))))
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
                        targets.Add(target);
                    }
                }
            }
        }

        return targets;
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
}
