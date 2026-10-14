using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// What <see cref="ConfigurationTestPattern"/> asks about the code: which AssertConfigurationIsValid calls, MapperConfiguration and
/// mapper variables, statements and test members can go once the profiles are hand-written methods.
/// </summary>
internal static class ConfigurationSyntax
{
    private const string AutoMapperAssembly = "AutoMapper";

    private static readonly string[] s_profileCalls = ["AddProfile", "AddProfiles", "AddMaps"];

    // Test methods and per-test or per-fixture setup of xUnit, NUnit and MSTest (and attributes derived from them, e.g. SkippableFact).
    private static readonly string[] s_testAttributes =
        ["FactAttribute", "TheoryAttribute", "TestAttribute", "TestCaseAttribute", "TestCaseSourceAttribute", "TestMethodAttribute",
            "DataTestMethodAttribute", "SetUpAttribute", "OneTimeSetUpAttribute", "TestInitializeAttribute"];

    private static readonly string[] s_testNamespaces = ["Xunit", "NUnit.Framework", "Microsoft.VisualStudio.TestTools.UnitTesting"];

    // Every node the pattern reports: removable test members, fields and statements, plus the configurations and checks that stay Manual.
    public static IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        HashSet<ISymbol> removable = RemovableSymbols(root, model);
        HashSet<StatementSyntax> statements = [.. root.DescendantNodes().OfType<StatementSyntax>().Where(s => IsRemovableStatement(s, model, removable))];
        List<SyntaxNode> found = [];
        HashSet<StatementSyntax> covered = [];
        foreach (BaseMethodDeclarationSyntax member in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Where(m => IsRemovableMember(m, model, statements)))
        {
            found.Add(member);
            covered.UnionWith(member.Body?.Statements ?? []);
        }

        found.AddRange(root.DescendantNodes().OfType<FieldDeclarationSyntax>()
            .Where(f => f.Declaration.Variables.All(v => model.GetDeclaredSymbol(v) is { } field && removable.Contains(field))));
        found.AddRange(statements.Where(s => !covered.Contains(s)));
        found.AddRange(root.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>().Where(c => IsConfiguration(c, model) && ConfigurationProblem(c, model) is not null));
        found.AddRange(root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i => IsAssert(i, model) && AssertProblem(i, model) is not null));
        return found;
    }

    // new MapperConfiguration(...) of AutoMapper.
    public static bool IsConfiguration(BaseObjectCreationExpressionSyntax creation, SemanticModel model) =>
        model.GetTypeInfo(creation).Type is { Name: "MapperConfiguration", ContainingAssembly.Name: AutoMapperAssembly };

    public static bool IsAssert(InvocationExpressionSyntax invocation, SemanticModel model) =>
        IsAutoMapperMethod(invocation, model, "AssertConfigurationIsValid");

    // Why the configuration does more than add profiles of this solution (its maps or settings would stay AutoMapper), or null.
    public static string? ConfigurationProblem(BaseObjectCreationExpressionSyntax creation, SemanticModel model)
    {
        if (creation.ArgumentList?.Arguments is not [{ Expression: LambdaExpressionSyntax lambda }]
            || model.GetSymbolInfo(lambda).Symbol is not IMethodSymbol { Parameters: [{ } parameter] })
        {
            return $"it is built from {Short(creation.ArgumentList?.ToString() ?? "()")}, not from a lambda that only adds profiles";
        }

        return lambda.Body switch
        {
            ExpressionSyntax expression => ProfileCallProblem(expression, parameter, model),
            BlockSyntax block => ProfileStatementsProblem(block.Statements, parameter, model),
            _ => "it has no body",
        };
    }

    // Why an AssertConfigurationIsValid call cannot simply be left out, or null.
    public static string? AssertProblem(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } || !MapCallPattern.HasNoSideEffects(receiver)
            || !invocation.ArgumentList.Arguments.All(a => IsConstantOrName(a.Expression, model)))
        {
            return $"'{Short(invocation.ToString())}' does more than check the configuration (a call in the receiver or the arguments)";
        }

        return invocation.Parent is ExpressionStatementSyntax { Parent: BlockSyntax }
            || (invocation.Parent is ArrowExpressionClauseSyntax { Parent: MethodDeclarationSyntax method } && IsRemovableTestMethod(method, model))
            ? null
            : $"'{Short(invocation.ToString())}' is not a statement of its own";
    }

    // Locals and private fields of an AutoMapper type (configuration or mapper) that are only built here from profiles, checked or mapped
    // with (Map calls belong to the Map patterns), so they are dead once the profiles are hand-written methods.
    public static HashSet<ISymbol> RemovableSymbols(SyntaxNode root, SemanticModel model)
    {
        Dictionary<ISymbol, List<ISymbol>?> candidates = new(SymbolEqualityComparer.Default);
        foreach (VariableDeclaratorSyntax declarator in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (model.GetDeclaredSymbol(declarator) is { } symbol && IsCandidate(declarator, symbol))
            {
                candidates[symbol] = [];
            }
        }

        List<IdentifierNameSyntax> names = [.. root.DescendantNodes().OfType<IdentifierNameSyntax>()];
        foreach (ISymbol symbol in candidates.Keys.ToList())
        {
            List<ExpressionSyntax> values = [.. symbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).OfType<VariableDeclaratorSyntax>()
                .Select(d => d.Initializer?.Value).OfType<ExpressionSyntax>()];
            List<ISymbol> links = [];
            bool removable = true;
            foreach (IdentifierNameSyntax name in names.Where(n => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, symbol)))
            {
                ExpressionSyntax reference = name.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } self && self.Name == name ? self : name;
                if (reference.Parent is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Parent: ExpressionStatementSyntax { Parent: BlockSyntax } } write
                    && write.Left == reference)
                {
                    values.Add(write.Right);
                }
                else if (Use(reference, model, candidates) is (true, var target))
                {
                    links.AddRange(target is null ? [] : [target]);
                }
                else
                {
                    removable = false;
                    break;
                }
            }

            // Every value is built here from profiles (or comes from another such variable, which must go too); at least one is
            // built here: an injected IMapper is not this pattern's business.
            removable &= values.Count > 0 && values.All(v => IsValue(v, model, candidates)) && values.Any(v => IsBuilt(v, model, candidates));
            links.AddRange(values.Select(v => Source(v, model, candidates)).OfType<ISymbol>());
            candidates[symbol] = removable ? links : null;
        }

        // A variable goes only together with the variables its value comes from or flows into.
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach ((ISymbol symbol, List<ISymbol>? targets) in candidates.Where(c => c.Value is not null).ToList())
            {
                if (targets!.Any(t => candidates[t] is null))
                {
                    candidates[symbol] = null;
                    changed = true;
                }
            }
        }

        return new HashSet<ISymbol>(candidates.Where(c => c.Value is not null).Select(c => c.Key), SymbolEqualityComparer.Default);
    }

    // A test method or setup (xUnit, NUnit, MSTest attribute) or a test class's only, public, parameterless constructor whose every
    // statement can go; or a test method whose expression body is an AssertConfigurationIsValid call.
    private static bool IsRemovableMember(BaseMethodDeclarationSyntax member, SemanticModel model, HashSet<StatementSyntax> statements)
    {
        bool eligible = member switch
        {
            MethodDeclarationSyntax method => IsRemovableTestMethod(method, model),
            ConstructorDeclarationSyntax constructor => IsLoneDefaultConstructor(constructor),
            _ => false,
        };
        if (!eligible)
        {
            return false;
        }

        return member.Body is { Statements: { Count: > 0 } body }
            ? body.All(statements.Contains)
            : member.ExpressionBody is { Expression: InvocationExpressionSyntax invocation } && IsAssert(invocation, model) && AssertProblem(invocation, model) is null;
    }

    private static bool IsLoneDefaultConstructor(ConstructorDeclarationSyntax constructor) =>
        constructor is { ParameterList.Parameters.Count: 0, Initializer: null, Parent: ClassDeclarationSyntax type }
        && constructor.Modifiers.Any(SyntaxKind.PublicKeyword) && !constructor.Modifiers.Any(SyntaxKind.StaticKeyword)
        && type.Members.OfType<ConstructorDeclarationSyntax>().Count(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword)) == 1;

    // An override or partial method is part of a contract; removing it changes more than the test.
    private static bool IsRemovableTestMethod(MethodDeclarationSyntax method, SemanticModel model) =>
        IsTestMember(method, model) && !method.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword) || m.IsKind(SyntaxKind.PartialKeyword));

    private static bool IsTestMember(MethodDeclarationSyntax method, SemanticModel model) =>
        method.AttributeLists.SelectMany(l => l.Attributes).Any(a => model.GetTypeInfo(a).Type is INamedTypeSymbol type && IsTestAttribute(type));

    private static bool IsTestAttribute(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
        {
            string ns = current.ContainingNamespace?.ToDisplayString() ?? "";
            if (s_testAttributes.Contains(current.Name, StringComparer.Ordinal)
                && s_testNamespaces.Any(t => ns == t || ns.StartsWith(t + ".", StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return false;
    }

    // A statement in a block that only checks the configuration, or declares or assigns a removable variable.
    private static bool IsRemovableStatement(StatementSyntax statement, SemanticModel model, HashSet<ISymbol> removable) => statement switch
    {
        { Parent: not BlockSyntax } => false,
        ExpressionStatementSyntax { Expression: InvocationExpressionSyntax invocation } => IsAssert(invocation, model) && AssertProblem(invocation, model) is null,
        ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Left: var left } } =>
            model.GetSymbolInfo(left).Symbol is { } symbol && removable.Contains(symbol),
        LocalDeclarationStatementSyntax { Declaration.Variables: [{ } variable] } =>
            model.GetDeclaredSymbol(variable) is { } local && removable.Contains(local),
        _ => false,
    };

    // A local (one per declaration, not using/const) or a private field of a non-partial type, typed as an AutoMapper configuration or mapper.
    private static bool IsCandidate(VariableDeclaratorSyntax declarator, ISymbol symbol) => symbol switch
    {
        ILocalSymbol { IsConst: false, IsUsing: false, Type: var type } => IsAutoMapperType(type)
            && declarator.Parent is VariableDeclarationSyntax { Variables.Count: 1, Parent: LocalDeclarationStatementSyntax { Parent: BlockSyntax } },
        IFieldSymbol { IsConst: false, DeclaredAccessibility: Accessibility.Private, Type: var type, ContainingType: var owner } => IsAutoMapperType(type)
            && owner.DeclaringSyntaxReferences.Length == 1,
        _ => false,
    };

    private static bool IsAutoMapperType(ITypeSymbol type) => type is INamedTypeSymbol { ContainingAssembly.Name: AutoMapperAssembly };

    // (true, null): the reference is checked (AssertConfigurationIsValid) or mapped with (Map, ProjectTo: the Map patterns' calls);
    // (true, target): it builds the value of candidate target (target = x, target = x.CreateMapper()); (false, _): it escapes.
    private static (bool Removable, ISymbol? Target) Use(ExpressionSyntax reference, SemanticModel model, Dictionary<ISymbol, List<ISymbol>?> candidates)
    {
        ExpressionSyntax value = reference;
        if (reference.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var name } access && access.Expression == reference)
        {
            if (access.Parent is InvocationExpressionSyntax call && call.Expression == access)
            {
                if (IsAutoMapperMethod(call, model, "Map") || IsAutoMapperMethod(call, model, "ProjectTo"))
                {
                    return (true, null);
                }

                if (IsAssert(call, model))
                {
                    return (AssertProblem(call, model) is null, null);
                }

                if (!IsAutoMapperMethod(call, model, "CreateMapper") || call.ArgumentList.Arguments.Count != 0)
                {
                    return (false, null);
                }

                value = call;
            }
            else if (name == "ConfigurationProvider" && access.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax check } && IsAssert(check, model))
            {
                return (AssertProblem(check, model) is null, null);
            }
            else
            {
                return (false, null);
            }
        }

        ISymbol? target = value.Parent switch
        {
            EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator } => model.GetDeclaredSymbol(declarator),
            AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Parent: ExpressionStatementSyntax { Parent: BlockSyntax } } write
                when write.Right == value => model.GetSymbolInfo(write.Left).Symbol,
            _ => null,
        };
        return target is not null && candidates.ContainsKey(target) ? (true, target) : (false, null);
    }

    // A value a removable variable may get: built here, or another candidate (whose use is checked on its own).
    private static bool IsValue(ExpressionSyntax value, SemanticModel model, Dictionary<ISymbol, List<ISymbol>?> candidates) =>
        IsBuilt(value, model, candidates)
        || (value is IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }
            && model.GetSymbolInfo(value).Symbol is { } symbol && candidates.ContainsKey(symbol));

    // The candidate a value comes from: x, this.x or x.CreateMapper().
    private static ISymbol? Source(ExpressionSyntax value, SemanticModel model, Dictionary<ISymbol, List<ISymbol>?> candidates) => value switch
    {
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: var receiver } } call when IsAutoMapperMethod(call, model, "CreateMapper") =>
            Source(receiver, model, candidates),
        ParenthesizedExpressionSyntax { Expression: var inner } => Source(inner, model, candidates),
        IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } =>
            model.GetSymbolInfo(value).Symbol is { } symbol && candidates.ContainsKey(symbol) ? symbol : null,
        _ => null,
    };

    // new MapperConfiguration(cfg => cfg.AddProfile...), or CreateMapper() on such a value or on a candidate.
    private static bool IsBuilt(ExpressionSyntax value, SemanticModel model, Dictionary<ISymbol, List<ISymbol>?> candidates) => value switch
    {
        BaseObjectCreationExpressionSyntax creation => IsConfiguration(creation, model) && ConfigurationProblem(creation, model) is null,
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: var receiver }, ArgumentList.Arguments.Count: 0 } call =>
            IsAutoMapperMethod(call, model, "CreateMapper") && IsValue(receiver, model, candidates),
        ParenthesizedExpressionSyntax { Expression: var inner } => IsBuilt(inner, model, candidates),
        _ => false,
    };

    private static string? ProfileStatementsProblem(IEnumerable<StatementSyntax> statements, IParameterSymbol parameter, SemanticModel model) =>
        statements.Select(s => s switch
        {
            ExpressionStatementSyntax { Expression: var expression } => ProfileCallProblem(expression, parameter, model),
            BlockSyntax block => ProfileStatementsProblem(block.Statements, parameter, model),
            ForEachStatementSyntax { Statement: var body } => ProfileStatementsProblem([body], parameter, model),
            _ => $"'{Short(s.ToString())}' is not an AddProfile or AddMaps call",
        }).FirstOrDefault(p => p is not null);

    // cfg.AddProfile<P>() / AddProfile(typeof(P)) / AddProfile(new P()) / AddProfiles(...) / AddMaps(...) on the lambda's parameter,
    // where every profile type it names is source code of the solution (a package's profile keeps its maps).
    private static string? ProfileCallProblem(ExpressionSyntax expression, IParameterSymbol parameter, SemanticModel model)
    {
        if (expression is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: var receiver } } invocation
            || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, parameter)
            || !s_profileCalls.Any(c => IsAutoMapperMethod(invocation, model, c)))
        {
            return $"'{Short(expression.ToString())}' is not an AddProfile or AddMaps call";
        }

        IEnumerable<ITypeSymbol?> named = ((IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!).TypeArguments
            .Concat(invocation.ArgumentList.DescendantNodes().OfType<TypeOfExpressionSyntax>().Select(t => model.GetTypeInfo(t.Type).Type))
            .Concat(invocation.ArgumentList.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Select(c => model.GetTypeInfo(c).Type));
        return named.FirstOrDefault(t => t is not null && !t.Locations.Any(l => l.IsInSource)) is { } external
            ? $"it adds {external.Name} from a referenced assembly, whose maps stay AutoMapper"
            : null;
    }

    private static bool IsConstantOrName(ExpressionSyntax expression, SemanticModel model) =>
        model.GetConstantValue(expression).HasValue || expression is TypeOfExpressionSyntax || MapCallPattern.HasNoSideEffects(expression);

    private static bool IsAutoMapperMethod(InvocationExpressionSyntax invocation, SemanticModel model, string name) =>
        model.GetSymbolInfo(invocation) is var info
        && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol method
        && method.Name == name && method.ContainingAssembly?.Name == AutoMapperAssembly;

    private static string Short(string code)
    {
        string single = string.Join(' ', code.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return single.Length <= 80 ? single : single[..77] + "...";
    }
}
