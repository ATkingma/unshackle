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
        "services.AddMediatR(cfg => ...) → services.AddMediator(cfg => ...): assembly registration dropped, behaviors → PipelineBehaviors, Lifetime → ServiceLifetime";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i =>
            model.GetSymbolInfo(i).Symbol is IMethodSymbol { Name: "AddMediatR" } method && method.ContainingAssembly.Name == "MediatR");

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var invocation = (InvocationExpressionSyntax)node;
        if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddMediatR" } member
            || invocation.ArgumentList.Arguments is not [{ Expression: LambdaExpressionSyntax lambda }]
            || ParameterName(lambda) is not string config)
        {
            return PatternResult.Manual("AddMediatR is not called as services.AddMediatR(cfg => ...); register Mediator with services.AddMediator(...) by hand.");
        }

        List<ExpressionSyntax> settings = lambda switch
        {
            { Block: { } block } when block.Statements.All(s => s is ExpressionStatementSyntax) =>
                [.. block.Statements.Cast<ExpressionStatementSyntax>().Select(s => s.Expression)],
            { ExpressionBody: { } expression } => [expression],
            _ => [],
        };
        if (settings.Count == 0)
        {
            return PatternResult.Manual("The AddMediatR configuration is not a list of cfg calls and assignments; configure AddMediator by hand.");
        }

        string lifetime = "ServiceLifetime.Transient"; // MediatR's default; Mediator's is Singleton.
        List<string> behaviors = [];
        foreach (ExpressionSyntax setting in settings)
        {
            switch (setting)
            {
                case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target } call } when target.Identifier.ValueText == config:
                    string name = call.Name.Identifier.ValueText;
                    if (s_dropped.Contains(name))
                    {
                        continue;
                    }

                    if (Behavior(call, ((InvocationExpressionSyntax)setting).ArgumentList) is not string behavior)
                    {
                        return PatternResult.Manual($"{config}.{name}(...) has no direct Mediator equivalent; configure AddMediator by hand.");
                    }

                    behaviors.Add(behavior);
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
                default:
                    return PatternResult.Manual($"'{setting}' has no direct Mediator equivalent; configure AddMediator by hand.");
            }
        }

        // Mediator's generator reads these assignments from source, so they stay plain literals.
        List<string> assignments = [$"{config}.ServiceLifetime = {lifetime}"];
        if (behaviors.Count > 0)
        {
            assignments.Add($"{config}.PipelineBehaviors = [{string.Join(", ", behaviors)}]");
        }

        LambdaExpressionSyntax? rewritten = lambda.Block is { } body
            ? lambda.WithBlock(body.WithStatements([.. assignments.Select(a => SyntaxFactory.ParseStatement(a + ";")
                .WithLeadingTrivia(body.Statements[0].GetLeadingTrivia())
                .WithTrailingTrivia(body.Statements[0].GetTrailingTrivia()))]))
            : assignments.Count == 1
                ? lambda.WithExpressionBody(SyntaxFactory.ParseExpression(assignments[0]).WithTriviaFrom(lambda.Body))
                : null;
        if (rewritten is null)
        {
            return PatternResult.Manual("Behaviors in an AddMediatR expression lambda; write AddMediator(cfg => { ... }) by hand.");
        }

        return PatternResult.Fixed(
            invocation
                .WithExpression(member.WithName(SyntaxFactory.IdentifierName("AddMediator").WithTriviaFrom(member.Name)))
                .WithArgumentList(invocation.ArgumentList.WithArguments([invocation.ArgumentList.Arguments[0].WithExpression(rewritten)])),
            "Microsoft.Extensions.DependencyInjection");
    }

    private static string? ParameterName(LambdaExpressionSyntax lambda) => lambda switch
    {
        SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.ValueText,
        ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [{ } parameter] } => parameter.Identifier.ValueText,
        _ => null,
    };

    // AddOpenBehavior(typeof(X<,>)), AddBehavior(typeof(X)), AddBehavior(typeof(I), typeof(X)), AddBehavior<X>(), AddBehavior<I, X>()
    // → typeof of the implementation; a lifetime argument or a computed type → null (Mediator has one lifetime for all).
    private static string? Behavior(MemberAccessExpressionSyntax call, ArgumentListSyntax arguments) => call.Name switch
    {
        IdentifierNameSyntax { Identifier.ValueText: "AddOpenBehavior" } when arguments.Arguments is [{ Expression: TypeOfExpressionSyntax type }] => type.ToString(),
        IdentifierNameSyntax { Identifier.ValueText: "AddBehavior" } when arguments.Arguments is [{ Expression: TypeOfExpressionSyntax type }] => type.ToString(),
        IdentifierNameSyntax { Identifier.ValueText: "AddBehavior" }
            when arguments.Arguments is [{ Expression: TypeOfExpressionSyntax }, { Expression: TypeOfExpressionSyntax type }] => type.ToString(),
        GenericNameSyntax { Identifier.ValueText: "AddBehavior" } generic when arguments.Arguments.Count == 0 =>
            $"typeof({generic.TypeArgumentList.Arguments[^1]})",
        _ => null,
    };
}
