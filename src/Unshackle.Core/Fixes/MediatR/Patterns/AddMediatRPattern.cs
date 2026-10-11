using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class AddMediatRPattern : IPattern
{
    // Mediator's source generator finds handlers at compile time; assembly scanning has nothing left to do.
    private static readonly HashSet<string> s_dropped =
        new(["RegisterServicesFromAssembly", "RegisterServicesFromAssemblies", "RegisterServicesFromAssemblyContaining"], StringComparer.Ordinal);

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

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
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

        LambdaExpressionSyntax? rewritten = lambda.Block is { } body
            ? body.Statements.Count == 0 && assignments.Count == 1
                ? lambda.WithBlock(null).WithExpressionBody(SyntaxFactory.ParseExpression(assignments[0]).WithTriviaFrom(body))
                : lambda.WithBlock(body.WithStatements(BlockStatements(body, assignments)))
            : assignments.Count == 1
                ? lambda.WithExpressionBody(SyntaxFactory.ParseExpression(assignments[0]).WithTriviaFrom(lambda.Body))
                : null;
        if (rewritten is null)
        {
            return PatternResult.Manual("Several settings in an AddMediatR expression lambda; write AddMediator(cfg => { ... }) by hand.");
        }

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
