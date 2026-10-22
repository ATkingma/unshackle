using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class AddMediatRPattern : ISolutionPattern
{
    // Mediator's source generator finds handlers at compile time; assembly scanning has nothing left to do.
    private static readonly HashSet<string> s_dropped =
        new(["RegisterServicesFromAssembly", "RegisterServicesFromAssemblies", "RegisterServicesFromAssemblyContaining"], StringComparer.Ordinal);

    private readonly IProjectProperties? _projectProperties;

    public AddMediatRPattern()
    {
    }

    // projectProperties: IsPackable per project (council round 10, R1); null guesses it from the compilation.
    public AddMediatRPattern(IProjectProperties? projectProperties)
    {
        _projectProperties = projectProperties;
    }

    public string Id => "mediatr.add-mediatr";

    public string Description =>
        "services.AddMediatR(cfg => ...) → services.AddMediator(cfg => ...): assembly registration dropped, behaviors, processors and exception handlers → PipelineBehaviors, stream behaviors → StreamPipelineBehaviors, Lifetime → ServiceLifetime";

    // A candidate counts too: once another file is fixed (AuditPreProcessor<> → <,>), the lambda no longer binds cleanly.
    // A call that is a whole statement is found as that statement, so a "_ = parameter;" can go in front of it (IDE0060).
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i =>
            model.GetSymbolInfo(i) is var info
            && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "AddMediatR" } method
            && method.ContainingAssembly.Name == "MediatR")
            .Select(i => i.Parent is ExpressionStatementSyntax statement ? statement : (SyntaxNode)i);

    // Without the solution every friend may call the internal method.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        Rewrite(node, model, Friend(model) is string friend ? $"{friend} may call" : null, null, _ => false, MediatorHostSettings.None, new HashSet<string>(), AssemblyResolution.None);

    // With the solution only a real call from another project counts, and the report names it (TAAK-148).
    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        string? caller = null;
        if (Friend(model) is not null
            && node.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>() is { } declaration
            && model.GetDeclaredSymbol(declaration, cancellationToken) is IMethodSymbol method)
        {
            IEnumerable<ReferencedSymbol> references = await SymbolFinder.FindReferencesAsync(method, solution, cancellationToken);
            caller = references.SelectMany(r => r.Locations)
                .Where(l => l.Document.Project.AssemblyName != model.Compilation.AssemblyName)
                .Select(l => $"{l.Document.Project.Name} calls ({l.Document.FilePath ?? l.Document.Name}:{l.Location.GetLineSpan().StartLinePosition.Line + 1})")
                .FirstOrDefault();
        }

        // An internal method whose scanned assembly comes from a public method's parameter scans whatever the caller passes (TAAK-290).
        string? forwarder = await PublicForwarderAsync(node, model, solution, cancellationToken);

        // A parameter or type parameter whose callers in the solution all pass the same literal assembly scans that one (council round 8, A2).
        AssemblyResolution resolution = await AssemblyResolver.ResolveAsync(DroppedScans(node), model, solution, _projectProperties, cancellationToken);

        // A host next to another host gets GenerateTypesAsInternal and/or Assemblies (TAAK-213); its Manual reason comes after the run-time scan checks.
        MediatorHostSettings host = await MediatorHosts.SettingsAsync(node, model, solution, cancellationToken);

        // A scanned project of this solution gets InternalsVisibleTo for this one from InternalHandlerAccessPattern (TAAK-195).
        IReadOnlySet<string> hosts = await MediatorHosts.AssemblyNamesAsync(solution, cancellationToken);
        return MediatorHosts.WithArchitectureTestWarning(Rewrite(node, model, caller, forwarder, a => CanGrantAccess(a, solution), host, hosts, resolution), solution);
    }

    // A C# project of the solution without a strong name: InternalHandlerAccessPattern can add InternalsVisibleTo to it.
    internal static bool CanGrantAccess(IAssemblySymbol assembly, Solution solution) =>
        !assembly.Identity.IsStrongName
        && solution.Projects.Any(p => p.Language == LanguageNames.CSharp && p.AssemblyName == assembly.Name);

    // caller: who may call this method from another project ("X may call", "X calls (file:line)"), or null.
    // granted: a scanned assembly that will give this project InternalsVisibleTo, so its internal handlers are fine.
    // forwarder: the public method whose parameter reaches the scanned assembly parameter of this non-public method, or null.
    // host: settings next to other hosts; hosts: assembly names of all hosts (a scan of another host cannot become Mediator's).
    // resolution: the assembly each parameter or type parameter of a scan resolves to through its callers, and why the others do not.
    private static PatternResult Rewrite(
        SyntaxNode node, SemanticModel model, string? caller, string? forwarder, Func<IAssemblySymbol, bool> granted, MediatorHostSettings host, IReadOnlySet<string> hosts,
        AssemblyResolution resolution)
    {
        var statement = node as ExpressionStatementSyntax;
        var invocation = (InvocationExpressionSyntax)(statement?.Expression ?? node);
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddMediatR" } member
            || invocation.ArgumentList.Arguments is not [{ Expression: LambdaExpressionSyntax lambda }]
            || ParameterName(lambda) is not string config)
        {
            return PatternResult.Manual("AddMediatR is not called as services.AddMediatR(cfg => ...); register Mediator with services.AddMediator(...) by hand.");
        }

        // An empty block is fine: AddMediator(cfg => { cfg.ServiceLifetime = ...; }).
        List<ExpressionSyntax>? settings = lambda switch
        {
            { Block: { } block } when block.Statements.All(s => s is ExpressionStatementSyntax) =>
                [.. block.Statements.Cast<ExpressionStatementSyntax>().Select(s => s.Expression)],
            { ExpressionBody: { } expression } => [expression],
            _ => null,
        };
        if (settings is null)
        {
            return PatternResult.Manual("The AddMediatR configuration is not a list of cfg calls and assignments; configure AddMediator by hand.");
        }

        string lifetime = "ServiceLifetime.Transient"; // MediatR's default; Mediator's is Singleton.
        List<string> behaviors = [];
        List<string> preProcessors = [];
        List<string> postProcessors = [];
        List<string> streamBehaviors = [];
        string? publisher = null;
        List<InvocationExpressionSyntax> dropped = [];
        foreach (ExpressionSyntax setting in settings)
        {
            switch (setting)
            {
                case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target } call } when target.Identifier.ValueText == config:
                    string name = call.Name.Identifier.ValueText;
                    if (s_dropped.Contains(name))
                    {
                        dropped.Add((InvocationExpressionSyntax)setting);
                        continue;
                    }

                    bool open = name.StartsWith("AddOpen", StringComparison.Ordinal);
                    List<string>? list = (open ? "Add" + name["AddOpen".Length..] : name) switch
                    {
                        "AddBehavior" => behaviors,
                        "AddStreamBehavior" => streamBehaviors,
                        "AddRequestPreProcessor" => preProcessors,
                        "AddRequestPostProcessor" => postProcessors,
                        _ => null,
                    };
                    if (list is null || Registered(call, ((InvocationExpressionSyntax)setting).ArgumentList, open) is not string type)
                    {
                        return PatternResult.Manual($"{config}.{name}(...) has no direct Mediator equivalent; configure AddMediator by hand.");
                    }

                    // Mediator's pre-processor also takes the response type: typeof(X<>) → typeof(X<,>) (RequestProcessorPattern adds TResponse).
                    list.Add(open && list == preProcessors ? type.Replace("<>", "<,>", StringComparison.Ordinal) : type);
                    break;
                case AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target, Name.Identifier.ValueText: "Lifetime" }, Right: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "ServiceLifetime" } } value }
                    when target.Identifier.ValueText == config:
                    lifetime = value.ToString();
                    break;
                // Mediator registers open generic notification handlers itself and has no generic request handlers
                // (UnsupportedPattern reports those), so the switch has nothing left to do.
                case AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target, Name.Identifier.ValueText: "RegisterGenericHandlers" } }
                    when target.Identifier.ValueText == config:
                    break;
                // MediatR's built-in publishers have twins in Mediator, set by type; a publisher of the user's own stays Manual.
                case AssignmentExpressionSyntax { Left: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target, Name.Identifier.ValueText: "NotificationPublisher" }, Right: ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0, Initializer: null } creation }
                    when target.Identifier.ValueText == config && IsBuiltInPublisher(model.GetTypeInfo(creation).Type, out string? publisherName):
                    publisher = $"{config}.NotificationPublisherType = typeof(global::Mediator.{publisherName})";
                    break;
                default:
                    return PatternResult.Manual($"'{setting}' has no direct Mediator equivalent; configure AddMediator by hand.");
            }
        }

        // The generator reads the configuration at compile time: an assembly named by a type parameter (AddMediatR in a generic
        // method, Miru's PipelineRegistry) is only known at run time (MSG0007).
        if (dropped.SelectMany(d => d.DescendantNodesAndSelf())
            .SelectMany(n => n switch
            {
                TypeOfExpressionSyntax t => [t.Type],
                GenericNameSyntax g => g.TypeArgumentList.Arguments,
                _ => (IEnumerable<TypeSyntax>)[],
            })
            .FirstOrDefault(t => model.GetTypeInfo(t).Type is ITypeParameterSymbol p && !resolution.Resolved.ContainsKey(p)) is TypeSyntax typeParameter)
        {
            return resolution.Manual(model.GetTypeInfo(typeParameter).Type!,
                $"AddMediatR scans the assembly of type parameter '{typeParameter}': Mediator's source generator needs the assemblies at compile time (MSG0007). "
                + "Call AddMediator once in the host project (with Mediator.SourceGenerator) instead of in this generic method.");
        }

        // Mediator's generator only sees this project and its references, and its code only reaches public handlers there (TAAK-144).
        if (CallerAssemblyParameter(dropped, model, IsVisibleOutside, resolution) is IParameterSymbol parameter)
        {
            return resolution.Manual(parameter,
                $"AddMediatR scans the assembly passed in '{parameter.Name}' of a public method: its handlers live in the caller's project, which Mediator's source generator in this project cannot see. "
                + "Call AddMediator once in the host project (with Mediator.SourceGenerator) instead of in this shared method.");
        }

        if (caller is not null && CallerAssemblyParameter(dropped, model, _ => true, resolution) is IParameterSymbol internalParameter)
        {
            return resolution.Manual(internalParameter,
                $"AddMediatR scans the assembly passed in '{internalParameter.Name}' of an internal method that {caller} (InternalsVisibleTo): its handlers would live in that project, which Mediator's source generator in this project cannot see. "
                + "Call AddMediator once in that project (with Mediator.SourceGenerator) instead.");
        }

        if (forwarder is not null && CallerAssemblyParameter(dropped, model, _ => true, resolution) is IParameterSymbol forwardedParameter)
        {
            return resolution.Manual(forwardedParameter,
                $"AddMediatR scans the assembly passed in '{forwardedParameter.Name}', which comes from a parameter of the public method {forwarder}: its handlers live in the caller's project and are only known at run time, "
                + "which Mediator's source generator in this project cannot see. Call AddMediator once in the host project (with Mediator.SourceGenerator) instead of in this shared method.");
        }

        // The scanned assemblies are known at compile time: now a clash with another host is the real blocker.
        if (host.ManualReason is string reason)
        {
            return PatternResult.Manual(reason);
        }

        if (ScannedAssemblies(dropped, model, resolution).FirstOrDefault(a => hosts.Contains(a.Name)) is IAssemblySymbol otherHost)
        {
            return PatternResult.Manual(
                $"AddMediatR scans {otherHost.Name}, which registers MediatR itself: both get Mediator's source generator, and its handlers cannot be generated in both (CS0436, CS0121 on AddMediator). "
                + "Call AddMediator in only one of the two projects.");
        }

        if (HiddenHandler(dropped, model, granted, resolution) is INamedTypeSymbol hidden)
        {
            return PatternResult.Manual(
                $"AddMediatR scans {hidden.ContainingAssembly.Name}, whose handler {hidden.Name} is not public: the code Mediator generates in this project cannot reach it (CS0122). "
                + "Make the handlers public, grant this project InternalsVisibleTo, or call AddMediator in the project that declares them.");
        }

        // MediatR's assembly scan registers closed exception handlers and processors itself; Mediator only runs what is listed.
        // Order: exception handlers outermost, then pre-processors, behaviors, post-processors.
        List<string> pipeline = [.. Scanned(model, [.. preProcessors, .. postProcessors]), .. preProcessors, .. behaviors, .. postProcessors];

        // Mediator's generator reads these assignments from source, so they stay plain literals.
        List<string> assignments = [$"{config}.ServiceLifetime = {lifetime}"];
        if (pipeline.Count > 0)
        {
            assignments.Add($"{config}.PipelineBehaviors = [{string.Join(", ", pipeline)}]");
        }

        if (streamBehaviors.Count > 0)
        {
            assignments.Add($"{config}.StreamPipelineBehaviors = [{string.Join(", ", streamBehaviors)}]");
        }

        if (publisher is not null)
        {
            assignments.Add(publisher);
        }

        assignments.AddRange(host.Assignments.Select(a => $"{config}.{a.Property} = {a.Value}"));

        LambdaExpressionSyntax rewritten = lambda.Block is { } body
            ? body.Statements.Count == 0 && assignments.Count == 1
                ? lambda.WithBlock(null).WithExpressionBody(SyntaxFactory.ParseExpression(assignments[0]).WithTriviaFrom(body))
                : lambda.WithBlock(body.WithStatements(BlockStatements(body, assignments)))
            : assignments.Count == 1
                ? lambda.WithExpressionBody(SyntaxFactory.ParseExpression(assignments[0]).WithTriviaFrom(lambda.Body))
                : ExpressionToBlock(lambda, assignments);

        InvocationExpressionSyntax addMediator = invocation
            .WithExpression(member.WithName(SyntaxFactory.IdentifierName("AddMediator").WithTriviaFrom(member.Name)))
            .WithArgumentList(invocation.ArgumentList.WithArguments([invocation.ArgumentList.Arguments[0].WithExpression(rewritten)]));
        List<string> unused = UnusedParameters(invocation, dropped, model);
        if (statement is not null)
        {
            // "_ = p;" keeps the signature and its callers intact and counts as a use for IDE0060.
            return PatternResult.Fixed(
                [.. unused.Select(p => SyntaxFactory.ParseStatement($"_ = {p};")), statement.WithExpression(addMediator)],
                "Microsoft.Extensions.DependencyInjection");
        }

        var result = (FixedResult)PatternResult.Fixed(addMediator, "Microsoft.Extensions.DependencyInjection");
        return unused.Count == 0
            ? result
            : result with
            {
                Warning = string.Join(" ", unused.Select(p =>
                    $"Parameter '{p}' is no longer used now that assembly scanning is gone (IDE0060); remove it and its argument at the callers."))
            };
    }

    // Parameters that were only passed to a dropped RegisterServicesFrom... call. Removing them changes the method's signature
    // and its callers: a statement gets "_ = p;" in front, any other call a warning in the report.
    private static List<string> UnusedParameters(InvocationExpressionSyntax invocation, List<InvocationExpressionSyntax> dropped, SemanticModel model)
    {
        if (invocation.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>() is not { } method)
        {
            return [];
        }

        List<IdentifierNameSyntax> references = [.. method.DescendantNodes().OfType<IdentifierNameSyntax>()];
        return [.. dropped.SelectMany(d => d.ArgumentList.DescendantNodes().OfType<IdentifierNameSyntax>())
            .Select(i => model.GetSymbolInfo(i).Symbol)
            .OfType<IParameterSymbol>()
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<IParameterSymbol>()
            .Where(p => references.All(r => dropped.Any(d => d.Span.Contains(r.Span))
                || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(r).Symbol, p)))
            .Select(p => p.Name)];
    }

    // The public method (as Type.Method) whose parameter is passed, through calls in the solution, to the assembly parameter a dropped
    // scan of this non-public method uses; null when every call passes an assembly known in its own project.
    private static async Task<string?> PublicForwarderAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        IReadOnlyList<InvocationExpressionSyntax> dropped = DroppedScans(node);
        Queue<IParameterSymbol> pending = new(dropped.SelectMany(d => d.ArgumentList.DescendantNodes().OfType<IdentifierNameSyntax>())
            .Select(i => model.GetSymbolInfo(i, cancellationToken).Symbol)
            .OfType<IParameterSymbol>());
        HashSet<ISymbol> seen = new(SymbolEqualityComparer.Default);
        while (pending.TryDequeue(out IParameterSymbol? parameter))
        {
            bool first = seen.Count == 0;
            if (parameter.ContainingSymbol is not IMethodSymbol method || !seen.Add(parameter))
            {
                continue;
            }

            // The method of the AddMediatR call itself is covered by the public-method check in Rewrite.
            if (IsVisibleOutside(method))
            {
                if (!first)
                {
                    return $"{method.ContainingType.Name}.{method.Name}";
                }

                continue;
            }

            foreach (ReferenceLocation location in (await SymbolFinder.FindReferencesAsync(method.OriginalDefinition, solution, cancellationToken)).SelectMany(r => r.Locations))
            {
                if ((await location.Document.GetSyntaxRootAsync(cancellationToken))?.FindNode(location.Location.SourceSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>() is { } call
                    && await location.Document.GetSemanticModelAsync(cancellationToken) is SemanticModel callModel
                    && Argument(call, callModel, parameter, cancellationToken) is { } argument
                    && callModel.GetSymbolInfo(argument, cancellationToken).Symbol is IParameterSymbol passed)
                {
                    pending.Enqueue(passed);
                }
            }
        }

        return null;
    }

    // The argument of a call that fills the parameter: by name, or by position (one less for an extension method called on its receiver).
    internal static ExpressionSyntax? Argument(InvocationExpressionSyntax call, SemanticModel model, IParameterSymbol parameter, CancellationToken cancellationToken)
    {
        if (call.ArgumentList.Arguments.FirstOrDefault(a => a.NameColon?.Name.Identifier.ValueText == parameter.Name) is { } named)
        {
            return named.Expression;
        }

        int position = parameter.Ordinal - (model.GetSymbolInfo(call, cancellationToken).Symbol is IMethodSymbol { ReducedFrom: not null } ? 1 : 0);
        return position >= 0 && position < call.ArgumentList.Arguments.Count && call.ArgumentList.Arguments[position].NameColon is null
            ? call.ArgumentList.Arguments[position].Expression
            : null;
    }

    // A parameter of a method passed to a dropped scan, when the method passes the check (e.g. externally visible) and its callers do not
    // all pass the same literal assembly: the scanned assembly is the caller's.
    private static IParameterSymbol? CallerAssemblyParameter(
        List<InvocationExpressionSyntax> dropped, SemanticModel model, Func<ISymbol, bool> callable, AssemblyResolution resolution) =>
        dropped.SelectMany(d => d.ArgumentList.DescendantNodes().OfType<IdentifierNameSyntax>())
            .Select(i => model.GetSymbolInfo(i).Symbol)
            .OfType<IParameterSymbol>()
            .FirstOrDefault(p => callable(p.ContainingSymbol) && !resolution.Resolved.ContainsKey(p));

    // A friend assembly (InternalsVisibleTo) can call an internal method too (TAAK-147); DynamicProxyGenAssembly2 is a mocking proxy, no caller.
    private static string? Friend(SemanticModel model) =>
        model.Compilation.Assembly.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.InternalsVisibleToAttribute")
            .Select(a => (a.ConstructorArguments.FirstOrDefault().Value as string)?.Split(',')[0].Trim())
            .FirstOrDefault(n => !string.IsNullOrEmpty(n) && n != "DynamicProxyGenAssembly2");

    // A non-public handler in another assembly that a dropped scan names by typeof(T) or <T>, directly or through a local.
    // An assembly that grants this project InternalsVisibleTo is fine: Mediator 3.0.2's generated code then reaches its internal handlers (TAAK-183).
    private static INamedTypeSymbol? HiddenHandler(
        List<InvocationExpressionSyntax> dropped, SemanticModel model, Func<IAssemblySymbol, bool> granted, AssemblyResolution resolution) =>
        ScannedAssemblies(dropped, model, resolution)
            .Where(a => !a.GivesAccessTo(model.Compilation.Assembly) && !granted(a))
            .SelectMany(a => Types(a.GlobalNamespace))
            .FirstOrDefault(IsHiddenHandler);

    // The other assemblies the AddMediatR call found by Find scans, for InternalHandlerAccessPattern; none when it is not services.AddMediatR(cfg => ...).
    internal static IReadOnlyList<IAssemblySymbol> ScannedAssemblies(SyntaxNode found, SemanticModel model) =>
        ScannedAssemblies([.. DroppedScans(found)], model, AssemblyResolution.None);

    // The RegisterServicesFrom... calls in the lambda of the AddMediatR call found by Find; none when it is not services.AddMediatR(cfg => ...).
    internal static IReadOnlyList<InvocationExpressionSyntax> DroppedScans(SyntaxNode found)
    {
        var invocation = (InvocationExpressionSyntax)((found as ExpressionStatementSyntax)?.Expression ?? found);
        if (invocation.ArgumentList.Arguments is not [{ Expression: LambdaExpressionSyntax lambda }] || ParameterName(lambda) is not string config)
        {
            return [];
        }

        IEnumerable<ExpressionSyntax> settings = lambda.Block?.Statements.OfType<ExpressionStatementSyntax>().Select(s => s.Expression)
            ?? (lambda.ExpressionBody is { } body ? [body] : []);
        return [.. settings.OfType<InvocationExpressionSyntax>().Where(i =>
            i.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target } call
            && target.Identifier.ValueText == config && s_dropped.Contains(call.Name.Identifier.ValueText))];
    }

    // Assemblies other than this one that the dropped scans name by typeof(T) or <T>, directly or through a local of this file,
    // or that their parameters and type parameters resolve to through the callers.
    private static IReadOnlyList<IAssemblySymbol> ScannedAssemblies(List<InvocationExpressionSyntax> dropped, SemanticModel model, AssemblyResolution resolution)
    {
        List<SyntaxNode> sources = [.. dropped];
        sources.AddRange(dropped.SelectMany(d => d.ArgumentList.DescendantNodes().OfType<IdentifierNameSyntax>())
            .Select(i => model.GetSymbolInfo(i).Symbol)
            .OfType<ILocalSymbol>()
            .SelectMany(l => l.DeclaringSyntaxReferences)
            .Select(r => r.GetSyntax())
            .OfType<VariableDeclaratorSyntax>()
            .Where(v => v.SyntaxTree == model.SyntaxTree)
            .Select(v => (SyntaxNode?)v.Initializer)
            .OfType<SyntaxNode>());
        return [.. sources
            .SelectMany(s => s.DescendantNodesAndSelf().OfType<TypeOfExpressionSyntax>().Select(t => t.Type)
                .Concat(s.DescendantNodesAndSelf().OfType<GenericNameSyntax>().SelectMany(g => g.TypeArgumentList.Arguments)))
            .Select(t => model.GetTypeInfo(t).Type?.ContainingAssembly)
            .OfType<IAssemblySymbol>()
            .Concat(resolution.Resolved.Values)
            .Where(a => !SymbolEqualityComparer.Default.Equals(a, model.Compilation.Assembly))
            .Distinct(SymbolEqualityComparer.Default)
            .Cast<IAssemblySymbol>()];
    }

    // A concrete MediatR (or already swapped Mediator) handler that code outside its assembly cannot name.
    internal static bool IsHiddenHandler(INamedTypeSymbol type) =>
        !type.IsAbstract && !IsVisibleOutside(type) && type.AllInterfaces.Any(i =>
            i.MetadataName is "IRequestHandler`1" or "IRequestHandler`2" or "INotificationHandler`1" or "IStreamRequestHandler`2"
            && i.ContainingNamespace.ToDisplayString() is "MediatR" or "Mediator");

    internal static IEnumerable<INamedTypeSymbol> Types(INamespaceOrTypeSymbol container) =>
        container.GetMembers().OfType<INamespaceOrTypeSymbol>().SelectMany(m => m is INamedTypeSymbol type ? [type, .. Types(type)] : Types(m));

    internal static bool IsVisibleOutside(ISymbol symbol)
    {
        for (ISymbol? current = symbol; current is not null and not INamespaceSymbol; current = current.ContainingSymbol)
        {
            if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
            {
                return false;
            }
        }

        return true;
    }

    // Each statement gets only the indentation of the block; comments of the old statements go in front of the first new one.
    private static SyntaxList<StatementSyntax> BlockStatements(BlockSyntax body, List<string> assignments)
    {
        SyntaxTriviaList indentation = body.Statements.Count > 0
            ? [.. body.Statements[0].GetLeadingTrivia().Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).TakeLast(1)]
            : [.. body.OpenBraceToken.LeadingTrivia.Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).TakeLast(1).Select(t => SyntaxFactory.Whitespace(t.ToString() + "    "))];
        SyntaxTrivia endOfLine = body.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) is { RawKind: not 0 } found ? found : SyntaxFactory.LineFeed;
        List<SyntaxTrivia> comments = [];
        foreach (StatementSyntax statement in body.Statements)
        {
            foreach (SyntaxTrivia trivia in statement.GetLeadingTrivia().Concat(statement.GetTrailingTrivia()).Where(IsComment))
            {
                comments.AddRange([.. indentation, trivia, endOfLine]);
            }
        }

        List<StatementSyntax> statements =
            [.. assignments.Select(a => SyntaxFactory.ParseStatement(a + ";").WithLeadingTrivia(indentation).WithTrailingTrivia(endOfLine))];
        statements[0] = statements[0].WithLeadingTrivia(comments.Concat(indentation));
        return [.. statements];
    }

    // cfg => cfg.A(...) with several settings left: the lambda becomes a block at the indentation of the line it starts on.
    private static LambdaExpressionSyntax ExpressionToBlock(LambdaExpressionSyntax lambda, List<string> assignments)
    {
        string line = lambda.SyntaxTree.GetText().Lines.GetLineFromPosition(lambda.SpanStart).ToString();
        string indent = line[..(line.Length - line.TrimStart().Length)];
        SyntaxTrivia endOfLine = lambda.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)) is { RawKind: not 0 } found ? found : SyntaxFactory.LineFeed;
        BlockSyntax block = SyntaxFactory.Block(
            SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.Whitespace(indent)).WithTrailingTrivia(endOfLine),
            [.. assignments.Select(a => SyntaxFactory.ParseStatement(a + ";").WithLeadingTrivia(SyntaxFactory.Whitespace(indent + "    ")).WithTrailingTrivia(endOfLine))],
            SyntaxFactory.Token(SyntaxKind.CloseBraceToken).WithLeadingTrivia(SyntaxFactory.Whitespace(indent)));
        return lambda.WithArrowToken(lambda.ArrowToken.WithTrailingTrivia(endOfLine)).WithExpressionBody(null).WithBlock(block);
    }

    private static bool IsComment(SyntaxTrivia trivia) =>
        trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia);

    // Closed exception handlers and pre/post-processors of this project that are not registered explicitly, as typeof(...).
    // shortcut: assumes the scanned assemblies are this project; scans of other assemblies are not followed, add that when users need it.
    private static List<string> Scanned(SemanticModel model, List<string> registered) =>
        [.. model.Compilation.SyntaxTrees
            .SelectMany(t => t.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
            .Select(c => model.Compilation.GetSemanticModel(c.SyntaxTree).GetDeclaredSymbol(c))
            .OfType<INamedTypeSymbol>()
            .Where(t => !t.IsAbstract && !t.IsGenericType && !registered.Contains($"typeof({t.Name})", StringComparer.Ordinal) && IsScanned(t))
            .Select(t => $"typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})")
            .Distinct(StringComparer.Ordinal)];

    // Files are fixed one by one: a handler may already derive from Mediator's base class (not bound yet, so by name).
    private static bool IsScanned(INamedTypeSymbol type) =>
        type.BaseType is { Name: "MessageExceptionHandler" or "MessagePreProcessor" or "MessagePostProcessor" }
        || type.Interfaces.Any(i => i.OriginalDefinition.MetadataName is "IRequestExceptionHandler`3" or "IRequestPreProcessor`1" or "IRequestPostProcessor`2"
            && i.ContainingNamespace.ToDisplayString() == "MediatR.Pipeline");

    // Mediator 3.0.2 has Mediator.TaskWhenAllPublisher and Mediator.ForeachAwaitPublisher; written as global::Mediator.X it does not
    // bind to MediatR while fixing, so using MediatR.NotificationPublishers counts as unused and goes (no CS0104 next to using Mediator).
    private static bool IsBuiltInPublisher(ITypeSymbol? type, out string? name)
    {
        name = type is { Name: "TaskWhenAllPublisher" or "ForeachAwaitPublisher" } && type.ContainingNamespace.ToDisplayString() == "MediatR.NotificationPublishers"
            ? type.Name
            : null;
        return name is not null;
    }

    private static string? ParameterName(LambdaExpressionSyntax lambda) => lambda switch
    {
        SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.ValueText,
        ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [{ } parameter] } => parameter.Identifier.ValueText,
        _ => null,
    };

    // AddOpenX(typeof(X<,>)), AddX(typeof(X)), AddX(typeof(I), typeof(X)), AddX<X>(), AddX<I, X>() for behaviors, stream behaviors and processors
    // → typeof of the implementation; a lifetime argument or a computed type → null (Mediator has one lifetime for all).
    private static string? Registered(MemberAccessExpressionSyntax call, ArgumentListSyntax arguments, bool open) => call.Name switch
    {
        IdentifierNameSyntax when arguments.Arguments is [{ Expression: TypeOfExpressionSyntax type }] => type.ToString(),
        IdentifierNameSyntax when !open && arguments.Arguments is [{ Expression: TypeOfExpressionSyntax }, { Expression: TypeOfExpressionSyntax type }] => type.ToString(),
        GenericNameSyntax generic when !open && arguments.Arguments.Count == 0 => $"typeof({generic.TypeArgumentList.Arguments[^1]})",
        _ => null,
    };
}
