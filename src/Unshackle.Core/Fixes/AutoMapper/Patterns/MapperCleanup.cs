using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Text;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// TAAK-289: which IMapper fields, constructor parameters, locals and mapper factory methods only serve calls the fix migrates, so they
/// can go (with the arguments for those parameters at every caller). Without them nothing asks DI for IMapper and AddAutoMapper can go too.
/// <para>
/// The plan is the largest set where every use of a removed symbol is a migrated Map, MapInto or ProjectTo call on it, or sits inside code
/// the plan removes (an assignment, an argument, a null check, a factory). Whatever is removed with a value must not do anything else: only
/// names, member accesses, <c>?? throw</c>, <c>CreateMapper()</c>, <c>new Mapper(..)</c>, a removed factory, and (when the solution calls
/// AddAutoMapper) <c>GetService&lt;IMapper&gt;()</c>/<c>GetRequiredService&lt;IMapper&gt;()</c> count. Anything else keeps the symbol.
/// </para>
/// </summary>
internal class MapperCleanup
{
    private const string AutoMapperAssembly = "AutoMapper";

    private static readonly string[] s_mapperTypes = ["AutoMapper.IMapper", "AutoMapper.IMapperBase", "AutoMapper.IRuntimeMapper"];

    private static readonly ConditionalWeakTable<SolutionId, Entry> s_cache = new();

    private readonly HashSet<string> _removed;

    private readonly MapperRegistration _registration;

    // Symbols a GetService<IMapper>() lookup feeds that would go without the scope rule of MapperRegistration (TAAK-292).
    private readonly HashSet<string> _keptByScope;

    private MapperCleanup(HashSet<string> removed, MapperRegistration registration, HashSet<string> keptByScope)
    {
        _removed = removed;
        _registration = registration;
        _keptByScope = keptByScope;
    }

    /// <summary>The plan without a solution: nothing is removed.</summary>
    public static MapperCleanup None { get; } = new([], MapperRegistration.None, []);

    /// <summary>
    /// The plan of the solution. Like <see cref="MapOwners.ForAsync"/> it is reused for a solution that differs only in the file it was built
    /// from and the file being fixed: the call patterns only remove uses that the plan allows anyway.
    /// </summary>
    public static async Task<MapperCleanup> ForAsync(Solution solution, SemanticModel model, CancellationToken cancellationToken)
    {
        Entry entry = s_cache.GetValue(solution.Id, _ => new Entry());
        Task<MapperCleanup> plan;
        lock (entry)
        {
            if (entry.Solution is null || entry.Plan is null || entry.Plan.IsFaulted || entry.Plan.IsCanceled
                || !MapOwners.SameApartFrom(solution, entry.Solution, [entry.FilePath, model.SyntaxTree.FilePath]))
            {
                entry.Solution = solution;
                entry.FilePath = model.SyntaxTree.FilePath;
                entry.Plan = BuildAsync(solution, model, cancellationToken);
            }

            plan = entry.Plan;
        }

        return await plan;
    }

    /// <summary>The outermost nodes of root the plan removes or trims, each with a warning when removing it may change behaviour.</summary>
    public IReadOnlyList<Removal> Removals(SyntaxNode root, SemanticModel model) => Removals(root, model, _removed, _registration);

    /// <summary>
    /// The GetService&lt;IMapper&gt;() lookups in root that stay only because their scope is not surely registered, each with the rule that failed.
    /// </summary>
    public IReadOnlyList<(InvocationExpressionSyntax Lookup, string Reason)> KeptLookups(SyntaxNode root, SemanticModel model)
    {
        if (_keptByScope.Count == 0)
        {
            return [];
        }

        return [.. root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(i => IsLookup(i, model) && Fed(i, model) is string key && _keptByScope.Contains(key))
            .Select(i => (i, $"{MapOwners.KeptPrefix}the GetService<IMapper>() lookup stays, IMapper may not be registered for this code ({_registration.Failure(model)}); "
                + "remove it and its null check by hand once AddAutoMapper surely runs first."))];
    }

    /// <summary>Whether node lies inside code the plan removes (a use of IMapper there does not need AddAutoMapper).</summary>
    public bool Removes(SyntaxNode node, SemanticModel model) =>
        _removed.Count > 0 && Removals(node.SyntaxTree.GetRoot(), model).Any(r => r.Node.Span.Contains(node.Span));

    /// <summary>Whether the parameter or variable declarator is one the plan removes.</summary>
    public bool IsRemoved(SyntaxNode declaration, SemanticModel model) => Key(model.GetDeclaredSymbol(declaration)) is string key && _removed.Contains(key);

    /// <summary>The arguments of a constructor call that go with the removed parameters.</summary>
    public IReadOnlyList<ArgumentSyntax> RemovedArguments(ArgumentListSyntax arguments, SemanticModel model) => RemovedArguments(arguments, model, _removed);

    internal static bool IsMapper(ITypeSymbol? type) =>
        type is INamedTypeSymbol { ContainingAssembly.Name: AutoMapperAssembly } named
        && s_mapperTypes.Contains(named.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(), StringComparer.Ordinal);

    // A symbol by a name that is the same in every compilation of the solution; null for one the plan never removes.
    private static string? Key(ISymbol? symbol) => symbol switch
    {
        IFieldSymbol field => field.OriginalDefinition.GetDocumentationCommentId(),
        IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } constructor } parameter
            => constructor.OriginalDefinition.GetDocumentationCommentId() is string id ? id + "#" + parameter.Name : null,
        ILocalSymbol local => local.ContainingSymbol.GetDocumentationCommentId() is string id ? id + "#local:" + local.Name : null,
        IMethodSymbol { MethodKind: MethodKind.Ordinary } method => method.OriginalDefinition.GetDocumentationCommentId(),
        _ => null,
    };

    private static List<Removal> Removals(SyntaxNode root, SemanticModel model, HashSet<string> removed, MapperRegistration registration)
    {
        List<Removal> found = [];
        if (removed.Count == 0)
        {
            return found;
        }

        bool Removed(ExpressionSyntax? expression) => expression is not null && Key(model.GetSymbolInfo(expression).Symbol) is string key && removed.Contains(key);
        bool Declared(SyntaxNode node) => Key(model.GetDeclaredSymbol(node)) is string key && removed.Contains(key);
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            switch (node)
            {
                case FieldDeclarationSyntax field when field.Declaration.Variables.Any(Declared):
                case LocalDeclarationStatementSyntax { Declaration.Variables: [var local] } when Declared(local):
                case MethodDeclarationSyntax method when Declared(method):
                    found.Add(new Removal(node, LookupWarning(node, model, registration)));
                    break;
                case ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment }
                    when Removed(Target(assignment.Left)) && IsDroppable(assignment.Right, model, removed, registration):
                    found.Add(new Removal(node, LookupWarning(node, model, registration)));
                    break;
                case ExpressionStatementSyntax { Expression: InvocationExpressionSyntax { ArgumentList.Arguments: [var checkedArgument, ..] } invocation }
                    when model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Name: "ThrowIfNull", ContainingType.Name: "ArgumentNullException" }
                        && Removed(checkedArgument.Expression):
                    found.Add(new Removal(node, null));
                    break;
                case IfStatementSyntax { Else: null, Parent: BlockSyntax } guard when NullChecked(guard.Condition) is { } checkedName && Removed(checkedName):
                    found.Add(new Removal(node, guard.Statement is ThrowStatementSyntax or BlockSyntax { Statements: [ThrowStatementSyntax] }
                        ? null
                        : $"The null check on {checkedName} is removed with the IMapper it checks: it only took its branch when no IMapper was "
                            + $"registered, and AddAutoMapper at {registration.Call(model)} registers one. Check that this code never ran without that registration."));
                    break;
                case ParameterListSyntax { Parent: ConstructorDeclarationSyntax } parameters when parameters.Parameters.Any(Declared):
                    found.Add(new Removal(node, null));
                    break;
                case ArgumentListSyntax arguments when RemovedArguments(arguments, model, removed).Count > 0:
                    found.Add(new Removal(node, null));
                    break;
            }
        }

        return [.. found.Where(r => !found.Any(o => o.Node != r.Node && o.Node.Span.Contains(r.Node.Span)))];
    }

    private static List<ArgumentSyntax> RemovedArguments(ArgumentListSyntax arguments, SemanticModel model, HashSet<string> removed)
    {
        if (arguments.Parent is not (BaseObjectCreationExpressionSyntax or ConstructorInitializerSyntax)
            || model.GetSymbolInfo(arguments.Parent).Symbol is not IMethodSymbol { MethodKind: MethodKind.Constructor } constructor)
        {
            return [];
        }

        return [.. constructor.Parameters.Where(p => Key(p) is string key && removed.Contains(key))
            .Select(p => Argument(arguments, p))
            .OfType<ArgumentSyntax>()];
    }

    // The argument that goes to parameter: named, or at its position before any named one.
    private static ArgumentSyntax? Argument(ArgumentListSyntax arguments, IParameterSymbol parameter) =>
        arguments.Arguments.FirstOrDefault(a => a.NameColon?.Name.Identifier.ValueText == parameter.Name)
        ?? (parameter.Ordinal < arguments.Arguments.Count && arguments.Arguments.Take(parameter.Ordinal + 1).All(a => a.NameColon is null)
            ? arguments.Arguments[parameter.Ordinal]
            : null);

    // _mapper or this._mapper.
    private static ExpressionSyntax Target(ExpressionSyntax expression) =>
        expression is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } member ? member.Name : expression;

    // x == null, null == x, x is null: the x.
    private static ExpressionSyntax? NullChecked(ExpressionSyntax condition) => condition switch
    {
        BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression, Right: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } } binary
            => Target(binary.Left),
        BinaryExpressionSyntax { RawKind: (int)SyntaxKind.EqualsExpression, Left: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } } binary
            => Target(binary.Right),
        IsPatternExpressionSyntax { Pattern: ConstantPatternSyntax { Expression: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression } } } pattern
            => Target(pattern.Expression),
        _ => null,
    };

    // null, default or null! as the first value of a field or local: nothing to drop. (Passed as an argument it may be what a test checks, so not there.)
    private static bool IsEmpty(ExpressionSyntax expression) => expression switch
    {
        PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed => IsEmpty(suppressed.Operand),
        LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression or (int)SyntaxKind.DefaultLiteralExpression } => true,
        _ => false,
    };

    // A value that may be dropped without dropping anything else the code does.
    private static bool IsDroppable(ExpressionSyntax expression, SemanticModel model, HashSet<string> removed, MapperRegistration registration) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => IsDroppable(parenthesized.Expression, model, removed, registration),
        PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed => IsDroppable(suppressed.Operand, model, removed, registration),
        CastExpressionSyntax cast => IsDroppable(cast.Expression, model, removed, registration),
        BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression, Right: ThrowExpressionSyntax } coalesce => IsDroppable(coalesce.Left, model, removed, registration),
        IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } => model.GetSymbolInfo(expression).Symbol is ILocalSymbol or IFieldSymbol or IParameterSymbol or IPropertySymbol,
        MemberAccessExpressionSyntax member => model.GetSymbolInfo(member).Symbol is IFieldSymbol or IPropertySymbol && IsDroppable(member.Expression, model, removed, registration),
        ObjectCreationExpressionSyntax creation => IsAutoMapperCreation(creation, model, removed, registration),
        InvocationExpressionSyntax invocation => model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && IsDroppableCall(invocation, method, model, removed, registration),
        _ => false,
    };

    // new MapperConfiguration(cfg => ...) or new Mapper(configuration): builds a mapper, nothing else.
    private static bool IsAutoMapperCreation(ObjectCreationExpressionSyntax creation, SemanticModel model, HashSet<string> removed, MapperRegistration registration) =>
        model.GetTypeInfo(creation).Type is { ContainingAssembly.Name: AutoMapperAssembly, Name: "MapperConfiguration" or "Mapper" }
        && (creation.ArgumentList?.Arguments ?? []).All(a => a.Expression is LambdaExpressionSyntax || IsDroppable(a.Expression, model, removed, registration));

    private static bool IsDroppableCall(InvocationExpressionSyntax invocation, IMethodSymbol method, SemanticModel model, HashSet<string> removed, MapperRegistration registration)
    {
        ExpressionSyntax? receiver = (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
        bool plainReceiver = receiver switch
        {
            null => true,
            ObjectCreationExpressionSyntax creation => IsAutoMapperCreation(creation, model, removed, registration),
            _ => model.GetSymbolInfo(receiver).Symbol is INamedTypeSymbol || IsDroppable(receiver, model, removed, registration),
        };
        if (!plainReceiver)
        {
            return false;
        }

        // CreateMapper() of a MapperConfiguration; GetService<IMapper>() only while AddAutoMapper registers it (else it returned null or threw).
        return method is { Name: "CreateMapper", ContainingAssembly.Name: AutoMapperAssembly }
            || (registration.Registers(model) && method is { Name: "GetService" or "GetRequiredService", TypeArguments: [var service] } && IsMapper(service))
            || (invocation.ArgumentList.Arguments.Count == 0 && Key(method) is string key && removed.Contains(key));
    }

    private static async Task<MapperCleanup> BuildAsync(Solution solution, SemanticModel current, CancellationToken cancellationToken)
    {
        MapOwners owners = await MapOwners.ForAsync(solution, current, cancellationToken);
        Dictionary<DocumentId, (SyntaxNode Root, SemanticModel Model)> documents = [];
        async Task<(SyntaxNode Root, SemanticModel Model)> LoadAsync(Document document)
        {
            if (!documents.TryGetValue(document.Id, out (SyntaxNode Root, SemanticModel Model) loaded))
            {
                loaded = ((await document.GetSyntaxRootAsync(cancellationToken))!, (await document.GetSemanticModelAsync(cancellationToken))!);
                documents[document.Id] = loaded;
            }

            return loaded;
        }

        // Candidates: every IMapper field, constructor parameter and local, and every static factory that only builds a mapper.
        Dictionary<string, (ISymbol Symbol, Document Document, SyntaxNode Declaration)> candidates = new(StringComparer.Ordinal);
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            if (!(await document.GetTextAsync(cancellationToken)).ToString().Contains("Mapper", StringComparison.Ordinal))
            {
                continue;
            }

            (SyntaxNode root, SemanticModel model) = await LoadAsync(document);
            foreach (SyntaxNode declaration in Declarations(root, model))
            {
                if (model.GetDeclaredSymbol(declaration, cancellationToken) is { } symbol && Key(symbol) is string key)
                {
                    // Two locals of one name in one member share a key: neither is removed.
                    if (!candidates.TryAdd(key, (symbol, document, declaration)))
                    {
                        candidates[key] = default;
                    }
                }
            }
        }

        foreach (string duplicate in candidates.Where(c => c.Value.Symbol is null).Select(c => c.Key).ToList())
        {
            candidates.Remove(duplicate);
        }

        // Every use of a candidate (and every call of a constructor with a candidate parameter), and the Map calls that may use one.
        Dictionary<string, List<ReferenceLocation>> uses = new(StringComparer.Ordinal);
        foreach ((string key, (ISymbol symbol, _, _)) in candidates)
        {
            ISymbol target = symbol is IParameterSymbol parameter ? parameter.ContainingSymbol : symbol;
            uses[key] = [.. (await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken)).SelectMany(r => r.Locations)];
            if (symbol is IParameterSymbol)
            {
                uses[key + "#calls"] = [.. (await SymbolFinder.FindReferencesAsync(target, solution, cancellationToken))
                    .Where(r => SymbolEqualityComparer.Default.Equals(r.Definition, target)).SelectMany(r => r.Locations)];
            }
        }

        Dictionary<DocumentId, HashSet<SyntaxNode>> migratedCalls = [];
        async Task<HashSet<SyntaxNode>> MigratedCallsAsync(Document document)
        {
            if (!migratedCalls.TryGetValue(document.Id, out HashSet<SyntaxNode>? calls))
            {
                (SyntaxNode root, SemanticModel model) = await LoadAsync(document);
                IEnumerable<SyntaxNode> found = new MapCallPattern().Find(root, model)
                    .Concat(new MapIntoPattern().Find(root, model))
                    .Concat(new ProjectToPattern().Find(root, model));
                calls = [.. found.Where(c => owners.KeptCall(c, model) is null)];
                migratedCalls[document.Id] = calls;
            }

            return calls;
        }

        MapperRegistration registration = await MapperRegistration.ForAsync(solution, cancellationToken);
        HashSet<string> removed = await PlanAsync(registration);

        // TAAK-292: the lookups the scope rule keeps are the ones the plan would remove if every project counted as registered.
        HashSet<string> keptByScope = registration.AnyFailure ? await PlanAsync(MapperRegistration.Everywhere) : [];
        keptByScope.ExceptWith(removed);
        return new MapperCleanup(removed, registration, keptByScope);

        async Task<HashSet<string>> PlanAsync(MapperRegistration registration)
        {
            HashSet<string> removed = [.. candidates.Keys];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Dictionary<DocumentId, List<Removal>> removals = [];
                async Task<bool> CoveredAsync(Document document, TextSpan span)
                {
                    (SyntaxNode root, SemanticModel model) = await LoadAsync(document);
                    if (!removals.TryGetValue(document.Id, out List<Removal>? inDocument))
                    {
                        inDocument = Removals(root, model, removed, registration);
                        removals[document.Id] = inDocument;
                    }

                    return inDocument.Any(r => r.Node.Span.Contains(span));
                }

                List<string> invalid = [];
                foreach (string key in removed)
                {
                    (ISymbol symbol, Document document, SyntaxNode declaration) = candidates[key];
                    if (!await IsValidAsync(key, symbol, document, declaration))
                    {
                        invalid.Add(key);
                    }
                }

                if (invalid.Count == 0)
                {
                    break;
                }

                removed.ExceptWith(invalid);

                async Task<bool> IsValidAsync(string key, ISymbol symbol, Document document, SyntaxNode declaration)
                {
                    (_, SemanticModel declarationModel) = await LoadAsync(document);
                    if (declaration is VariableDeclaratorSyntax { Initializer.Value: var initial } && !IsEmpty(initial) && !IsDroppable(initial, declarationModel, removed, registration))
                    {
                        return false;
                    }

                    foreach (ReferenceLocation use in uses[key])
                    {
                        if (use.IsImplicit || use.IsCandidateLocation)
                        {
                            return false;
                        }

                        (SyntaxNode root, _) = await LoadAsync(use.Document);
                        SyntaxNode node = root.FindNode(use.Location.SourceSpan, getInnermostNodeForTie: true);
                        if (!await CoveredAsync(use.Document, node.Span) && !(Call(node) is { } call && (await MigratedCallsAsync(use.Document)).Contains(call)))
                        {
                            return false;
                        }
                    }

                    return symbol is not IParameterSymbol parameter || await IsRemovableParameterAsync(key, parameter);
                }

                // Every call of the constructor passes a droppable value (or nothing) for it, and no other constructor is left with the same signature.
                async Task<bool> IsRemovableParameterAsync(string key, IParameterSymbol parameter)
                {
                    var constructor = (IMethodSymbol)parameter.ContainingSymbol;
                    foreach (ReferenceLocation call in uses[key + "#calls"])
                    {
                        (SyntaxNode root, SemanticModel model) = await LoadAsync(call.Document);
                        SyntaxNode node = root.FindNode(call.Location.SourceSpan, getInnermostNodeForTie: true);
                        ArgumentListSyntax? arguments = node.AncestorsAndSelf()
                            .Select(n => n switch
                            {
                                BaseObjectCreationExpressionSyntax creation => creation.ArgumentList,
                                ConstructorInitializerSyntax initializer => initializer.ArgumentList,
                                _ => null,
                            })
                            .FirstOrDefault(a => a is not null);
                        if (call.IsImplicit || arguments is null
                            || (model.GetSymbolInfo(arguments.Parent!, cancellationToken).Symbol as IMethodSymbol)?.OriginalDefinition.GetDocumentationCommentId() != constructor.OriginalDefinition.GetDocumentationCommentId()
                            || (Argument(arguments, parameter) is { } argument
                                && (!argument.RefKindKeyword.IsKind(SyntaxKind.None) || !IsDroppable(argument.Expression, model, removed, registration))))
                        {
                            return false;
                        }
                    }

                    List<string> left = [.. constructor.Parameters.Where(p => Key(p) is not string k || !removed.Contains(k)).Select(p => p.Type.ToDisplayString())];
                    return !constructor.ContainingType.InstanceConstructors.Any(c => !SymbolEqualityComparer.Default.Equals(c, constructor)
                        && c.Parameters.Select(p => p.Type.ToDisplayString()).SequenceEqual(left, StringComparer.Ordinal));
                }
            }

            return removed;
        }
    }

    // GetService<IMapper>() or GetRequiredService<IMapper>().
    private static bool IsLookup(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { Name: "GetService" or "GetRequiredService", TypeArguments: [var service] } && IsMapper(service);

    // The key of the field or local whose value (initial or assigned) the expression is part of; null when it feeds no such symbol.
    private static string? Fed(ExpressionSyntax expression, SemanticModel model)
    {
        foreach (SyntaxNode ancestor in expression.Ancestors().TakeWhile(a => a is not StatementSyntax and not MemberDeclarationSyntax || a is LocalDeclarationStatementSyntax or ExpressionStatementSyntax or FieldDeclarationSyntax))
        {
            switch (ancestor)
            {
                case VariableDeclaratorSyntax declarator:
                    return Key(model.GetDeclaredSymbol(declarator));
                case AssignmentExpressionSyntax assignment when assignment.Right.Span.Contains(expression.Span):
                    return Key(model.GetSymbolInfo(Target(assignment.Left)).Symbol);
            }
        }

        return null;
    }

    // The warning for a removed declaration or assignment whose value was a DI lookup: it names the AddAutoMapper call that registers IMapper.
    private static string? LookupWarning(SyntaxNode removed, SemanticModel model, MapperRegistration registration) =>
        removed.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => IsLookup(i, model))
            ? $"The GetService<IMapper>() lookup is removed: AddAutoMapper at {registration.Call(model)} registers IMapper for this code."
            : null;

    // The Map call (or method group) a use of a mapper is the receiver of: mapper.Map<B>(a), this.mapper.Map(a, b), items.Select(mapper.Map<B>).
    private static SyntaxNode? Call(SyntaxNode use)
    {
        SyntaxNode receiver = use.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } qualified && qualified.Name == use ? qualified : use;
        return receiver.Parent is MemberAccessExpressionSyntax member && member.Expression == receiver
            ? member.Parent is InvocationExpressionSyntax invocation && invocation.Expression == member ? invocation : member
            : null;
    }

    private static IEnumerable<SyntaxNode> Declarations(SyntaxNode root, SemanticModel model)
    {
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            switch (node)
            {
                case VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax } field when model.GetDeclaredSymbol(field) is IFieldSymbol { IsConst: false } symbol && IsMapper(symbol.Type):
                    yield return field;
                    break;
                case VariableDeclaratorSyntax { Parent.Parent: LocalDeclarationStatementSyntax { UsingKeyword.RawKind: (int)SyntaxKind.None, IsConst: false, Declaration.Variables.Count: 1 } } local
                    when model.GetDeclaredSymbol(local) is ILocalSymbol symbol && IsMapper(symbol.Type):
                    yield return local;
                    break;
                case ParameterSyntax { Parent.Parent: ConstructorDeclarationSyntax { Parent: ClassDeclarationSyntax } constructor } parameter
                    when !constructor.Modifiers.Any(SyntaxKind.StaticKeyword) && model.GetDeclaredSymbol(parameter) is IParameterSymbol { RefKind: RefKind.None, IsParams: false } symbol
                        && IsMapper(symbol.Type):
                    yield return parameter;
                    break;
                case MethodDeclarationSyntax method when IsFactory(method, model):
                    yield return method;
                    break;
            }
        }
    }

    // static IMapper Create() { var configuration = new MapperConfiguration(...); return configuration.CreateMapper(); }: only builds a mapper.
    private static bool IsFactory(MethodDeclarationSyntax method, SemanticModel model) =>
        method.Modifiers.Any(SyntaxKind.StaticKeyword) && method.ParameterList.Parameters.Count == 0 && method.TypeParameterList is null
        && !method.Modifiers.Any(SyntaxKind.PartialKeyword) && !method.Modifiers.Any(SyntaxKind.ExternKeyword)
        && IsMapper(model.GetTypeInfo(method.ReturnType).Type)
        && (method.ExpressionBody is not null || (method.Body is { } body && body.Statements.All(s => s is LocalDeclarationStatementSyntax or ReturnStatementSyntax)));

    /// <summary>A node the cleanup removes or trims, with a warning for the report when it may change behaviour.</summary>
    internal class Removal
    {
        public Removal(SyntaxNode node, string? warning)
        {
            Node = node;
            Warning = warning;
        }

        public SyntaxNode Node { get; }

        public string? Warning { get; }
    }

    private class Entry
    {
        public Solution? Solution { get; set; }

        public string? FilePath { get; set; }

        public Task<MapperCleanup>? Plan { get; set; }
    }
}
