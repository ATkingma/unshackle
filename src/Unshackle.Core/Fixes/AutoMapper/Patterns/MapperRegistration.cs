using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// TAAK-292 (council decision 3, 2026-10-19): whether <c>AddAutoMapper</c> surely registers IMapper for the code of a project, so a
/// <c>GetService&lt;IMapper&gt;()</c> lookup there (and its null check) may go. The scope of a project is the project itself plus every project
/// that transitively depends on it. It is registered only when (1) an AddAutoMapper call is in the scope, (2) one of them is unconditional
/// (not inside <c>if</c>, <c>?:</c>, <c>switch</c>, <c>#if</c> or a loop), and (3) no project in the scope builds a container
/// (<c>new ServiceCollection()</c>, <c>Host.CreateApplicationBuilder</c>, <c>WebApplication.CreateBuilder</c>) in a method body without an
/// AddAutoMapper call (syntactic). Otherwise <see cref="Failure"/> names the first condition that fails, with file:line.
/// <para>
/// Council round 3 (TAAK-293): a target-typed <c>new()</c> counts as a container through its converted type, and an AddAutoMapper call inside
/// a lambda or local function counts as unconditional only when the lambda is passed directly to an unconditional registration or builder
/// call; the failure carries a typed <see cref="KeptReason"/>.
/// </para>
/// </summary>
internal class MapperRegistration
{
    private const string AddAutoMapper = "AddAutoMapper";

    // The types a registration or builder call works on: a lambda passed to such a call runs while the container is built. Council round 4
    // (Q3): matched on the fully qualified metadata name, so a lookalike type in user code does not count.
    private static readonly HashSet<string> s_registrationTargets = new(StringComparer.Ordinal)
    {
        "Microsoft.Extensions.DependencyInjection.IServiceCollection",
        "Microsoft.Extensions.Hosting.IHostBuilder",
        "Microsoft.Extensions.Hosting.IHostApplicationBuilder",
        "Microsoft.Extensions.Hosting.HostApplicationBuilder",
        "Microsoft.AspNetCore.Hosting.IWebHostBuilder",
        "Microsoft.AspNetCore.Builder.WebApplicationBuilder",
        "Microsoft.Azure.Functions.Extensions.DependencyInjection.IFunctionsHostBuilder",
        "Microsoft.Azure.Functions.Worker.Builder.FunctionsApplicationBuilder",
        "Microsoft.Azure.Functions.Worker.IFunctionsWorkerApplicationBuilder",
    };

    // Assembly name → the file:line of the unconditional AddAutoMapper call that registers it, or the reason it is not registered.
    private readonly Dictionary<string, MapperRegistrationScope> _scopes;

    // Every project counts as registered: the plan as it would be without the scope rule, to find the lookups that rule keeps.
    private readonly bool _everywhere;

    private MapperRegistration(Dictionary<string, MapperRegistrationScope> scopes, bool everywhere)
    {
        _scopes = scopes;
        _everywhere = everywhere;
    }

    /// <summary>Nothing is registered: no lookup goes.</summary>
    public static MapperRegistration None { get; } = new([], everywhere: false);

    /// <summary>Everything counts as registered (only to compare plans).</summary>
    public static MapperRegistration Everywhere { get; } = new([], everywhere: true);

    /// <summary>Whether a scope fails for some project (only then can a lookup be kept because of the scope).</summary>
    public bool AnyFailure => _scopes.Values.Any(s => s.Failure is not null);

    public static async Task<MapperRegistration> ForAsync(Solution solution, CancellationToken cancellationToken)
    {
        Dictionary<ProjectId, MapperRegistrationFacts> facts = [];
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp))
        {
            facts[project.Id] = await FactsAsync(project, cancellationToken);
        }

        ProjectDependencyGraph graph = solution.GetProjectDependencyGraph();
        Dictionary<string, MapperRegistrationScope> scopes = new(StringComparer.Ordinal);
        foreach (Project project in solution.Projects.Where(p => p.Language == LanguageNames.CSharp))
        {
            List<Project> inScope = [project, .. graph.GetProjectsThatTransitivelyDependOnThisProject(project.Id)
                .Select(solution.GetProject).OfType<Project>().Where(p => facts.ContainsKey(p.Id)).OrderBy(p => p.Name, StringComparer.Ordinal)];
            MapperRegistrationScope scope = ScopeOf(project, inScope.Select(p => facts[p.Id]).ToList());

            // Projects that share an assembly name (multi-targeting) are registered only when all of them are.
            if (!scopes.TryGetValue(project.AssemblyName, out MapperRegistrationScope? known) || (known.Failure is null && scope.Failure is not null))
            {
                scopes[project.AssemblyName] = scope;
            }
        }

        return new MapperRegistration(scopes, everywhere: false);
    }

    /// <summary>Whether IMapper is surely registered for the code of model's project.</summary>
    public bool Registers(SemanticModel model) => _everywhere || Of(model) is { Failure: null };

    /// <summary>The file:line of the AddAutoMapper call that registers IMapper for model's project; null when it is not registered.</summary>
    public string? Call(SemanticModel model) => Of(model) is { Failure: null } scope ? scope.Call : null;

    /// <summary>Why IMapper is not surely registered for model's project, as a reason code and text; null when it is.</summary>
    public (KeptReason Code, string Text)? Failure(SemanticModel model) =>
        _everywhere ? null
        : Of(model) is not { } scope ? (KeptReason.NotInSolution, "the project is not part of the solution")
        : scope.Failure is string failure ? (scope.Code, failure)
        : null;

    private MapperRegistrationScope? Of(SemanticModel model) =>
        model.Compilation.AssemblyName is string name && _scopes.TryGetValue(name, out MapperRegistrationScope? scope) ? scope : null;

    private static MapperRegistrationScope ScopeOf(Project project, List<MapperRegistrationFacts> inScope)
    {
        List<(string Location, string? Condition)> calls = [.. inScope.SelectMany(f => f.Calls)];
        if (calls.Count == 0)
        {
            return new MapperRegistrationScope(null, $"rule 1: no AddAutoMapper call in {project.Name} or a project that depends on it", KeptReason.NoRegistration);
        }

        if (calls.FirstOrDefault(c => c.Condition is null) is not { Location: string call })
        {
            return new MapperRegistrationScope(null, $"rule 2: the AddAutoMapper call at {calls[0].Location} is conditional or deferred (inside {calls[0].Condition})",
                KeptReason.ConditionalOrDeferredRegistration);
        }

        if (inScope.SelectMany(f => f.Containers).FirstOrDefault() is string container)
        {
            return new MapperRegistrationScope(null, $"rule 3: {container} without AddAutoMapper in the same method, so IMapper may not be registered there",
                KeptReason.ContainerWithoutRegistration);
        }

        return new MapperRegistrationScope(call, null, KeptReason.None);
    }

    private static async Task<MapperRegistrationFacts> FactsAsync(Project project, CancellationToken cancellationToken)
    {
        var facts = new MapperRegistrationFacts();
        foreach (Document document in project.Documents)
        {
            string text = (await document.GetTextAsync(cancellationToken)).ToString();
            bool calls = text.Contains(AddAutoMapper, StringComparison.Ordinal);
            bool targetTyped = text.Contains("new(", StringComparison.Ordinal);
            if (!calls && !targetTyped && !text.Contains("ServiceCollection", StringComparison.Ordinal) && !text.Contains("CreateApplicationBuilder", StringComparison.Ordinal)
                && !text.Contains("CreateBuilder", StringComparison.Ordinal))
            {
                continue;
            }

            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            SemanticModel? model = calls || (targetTyped && root.DescendantNodes().OfType<ImplicitObjectCreationExpressionSyntax>().Any())
                ? await document.GetSemanticModelAsync(cancellationToken)
                : null;
            foreach (SyntaxNode node in root.DescendantNodes())
            {
                switch (node)
                {
                    case InvocationExpressionSyntax invocation when calls && model is not null
                        && model.GetSymbolInfo(invocation, cancellationToken).Symbol is IMethodSymbol { Name: AddAutoMapper, ContainingAssembly.Name: "AutoMapper" }:
                        facts.Calls.Add((Location(invocation), Condition(invocation, model, cancellationToken)));
                        break;
                    case ObjectCreationExpressionSyntax creation when LastName(creation.Type) == "ServiceCollection" && !CallsAddAutoMapper(Body(creation)):
                        facts.Containers.Add($"{Location(creation)} builds a new ServiceCollection()");
                        break;

                    // Council round 3 (Q3a): not recognising a target-typed container would err the unsafe way.
                    case ImplicitObjectCreationExpressionSyntax creation when model is not null && IsContainer(model.GetTypeInfo(creation, cancellationToken))
                        && !CallsAddAutoMapper(Body(creation)):
                        facts.Containers.Add($"{Location(creation)} builds a new ServiceCollection() (target-typed new())");
                        break;
                    case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation
                        when (LastName(member.Expression), member.Name.Identifier.ValueText) is ("Host", "CreateApplicationBuilder") or ("WebApplication", "CreateBuilder")
                            && !CallsAddAutoMapper(Body(invocation)):
                        facts.Containers.Add($"{Location(invocation)} calls {LastName(member.Expression)}.{member.Name.Identifier.ValueText}");
                        break;
                }
            }
        }

        return facts;
    }

    private static string Location(SyntaxNode node) =>
        $"{Path.GetFileName(node.SyntaxTree.FilePath)}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}";

    private static string? LastName(SyntaxNode node) => node switch
    {
        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
        MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
        AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
        SimpleNameSyntax name => name.Identifier.ValueText,
        _ => null,
    };

    // The method body the node is in: a method, constructor, accessor or local function, or the top-level statements of the file.
    private static SyntaxNode Body(SyntaxNode node) =>
        node.Ancestors().FirstOrDefault(a => a is BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AccessorDeclarationSyntax
            or PropertyDeclarationSyntax or FieldDeclarationSyntax)
        ?? node.SyntaxTree.GetRoot();

    private static bool CallsAddAutoMapper(SyntaxNode body) =>
        body.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => LastName(i.Expression) == AddAutoMapper);

    // Council round 5 (Q2): matched on the fully qualified name, like the registration targets, so a lookalike type in user code does not count.
    private static bool IsContainer(TypeInfo info) =>
        new[] { info.Type, info.ConvertedType }.OfType<ITypeSymbol>().Any(t => QualifiedName(t) is "Microsoft.Extensions.DependencyInjection.ServiceCollection"
            or "Microsoft.Extensions.DependencyInjection.IServiceCollection");

    // What makes the call conditional or deferred within its member, or null when it is not. A lambda counts only when it is passed directly
    // to a registration or builder call (council round 3, Q3b); the walk then goes on over that call's own ancestors.
    private static string? Condition(SyntaxNode call, SemanticModel model, CancellationToken cancellationToken)
    {
        foreach (SyntaxNode ancestor in call.Ancestors().TakeWhile(a => a is not MemberDeclarationSyntax))
        {
            string? condition = ancestor switch
            {
                LocalFunctionStatementSyntax => "a local function, which may run later or not at all",
                AnonymousFunctionExpressionSyntax lambda when !IsPassedToRegistration(lambda, model, cancellationToken)
                    => "a lambda that is not passed directly to a registration or builder call, so it may run later or not at all",
                IfStatementSyntax or ElseClauseSyntax => "an if",
                ConditionalExpressionSyntax => "a ?: expression",
                SwitchStatementSyntax or SwitchExpressionSyntax => "a switch",
                ForStatementSyntax or CommonForEachStatementSyntax or WhileStatementSyntax or DoStatementSyntax => "a loop",
                _ => null,
            };
            if (condition is not null)
            {
                return condition;
            }
        }

        int depth = 0;
        for (DirectiveTriviaSyntax? directive = call.SyntaxTree.GetRoot(cancellationToken).GetFirstDirective(); directive is not null && directive.SpanStart < call.SpanStart;
            directive = directive.GetNextDirective())
        {
            depth += directive.Kind() switch
            {
                SyntaxKind.IfDirectiveTrivia => 1,
                SyntaxKind.EndIfDirectiveTrivia => -1,
                _ => 0,
            };
        }

        return depth > 0 ? "an #if" : null;
    }

    // lambda is an argument of a call on, or extending, a service collection or a host, web host or application builder.
    private static bool IsPassedToRegistration(AnonymousFunctionExpressionSyntax lambda, SemanticModel model, CancellationToken cancellationToken)
    {
        if (lambda.Parent is not ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } })
        {
            return false;
        }

        SymbolInfo info = model.GetSymbolInfo(invocation, cancellationToken);
        if ((info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is not IMethodSymbol method)
        {
            return false;
        }

        ITypeSymbol? receiver = invocation.Expression is MemberAccessExpressionSyntax member ? model.GetTypeInfo(member.Expression, cancellationToken).Type : null;
        ITypeSymbol? extended = method.ReducedFrom is { Parameters: [var self, ..] } ? self.Type : null;
        return new[] { receiver, extended }.OfType<ITypeSymbol>().Any(IsRegistrationTarget);
    }

    private static bool IsRegistrationTarget(ITypeSymbol type) =>
        s_registrationTargets.Contains(QualifiedName(type)) || type.AllInterfaces.Any(i => s_registrationTargets.Contains(QualifiedName(i)));

    private static string QualifiedName(ITypeSymbol type) =>
        type.ContainingNamespace is { IsGlobalNamespace: false } ns ? $"{ns.ToDisplayString()}.{type.MetadataName}" : type.MetadataName;
}
