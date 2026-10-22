using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.Polly.Patterns;

/// <summary>
/// <c>.AddPolicyHandler(&lt;inline retry&gt;)</c> and <c>.AddTransientHttpErrorPolicy(p =&gt; p.&lt;retry&gt;)</c> →
/// <c>.AddResilienceHandler("retry", builder =&gt; builder.AddRetry(new RetryStrategyOptions&lt;HttpResponseMessage&gt; { ... }))</c>
/// (queue row 6). Plain <c>RetryStrategyOptions</c>, not <c>HttpRetryStrategyOptions</c> (that honours Retry-After), and the v7
/// transient-error filter written out, not <c>HttpClientResiliencePredicates</c> (that also retries 429 and timeouts).
/// </summary>
public class AddPolicyHandlerPattern : IPattern
{
    public const string HandlerName = "retry";

    private const string PolicyHttpAssembly = "Microsoft.Extensions.Http.Polly";
    private const string BuilderName = "builder";

    private static readonly string[] s_namespaces =
        ["System", "System.Net", "System.Net.Http", "System.Threading.Tasks", "Polly", "Polly.Retry", "Microsoft.Extensions.DependencyInjection"];

    // Handlers whose order relative to this one a rewrite could change.
    private static readonly HashSet<string> s_handlerMethods =
        new(["AddPolicyHandler", "AddTransientHttpErrorPolicy", "AddPolicyHandlerFromRegistry", "AddResilienceHandler", "AddStandardResilienceHandler", "AddStandardHedgingHandler"], StringComparer.Ordinal);

    public string Id => "polly.add-policy-handler";

    public string Description => ".AddPolicyHandler(<inline retry>) / .AddTransientHttpErrorPolicy(p => p.<retry>) → .AddResilienceHandler(\"retry\", builder => builder.AddRetry(...))";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        List<InvocationExpressionSyntax> handlers =
        [
            .. root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i =>
                i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddPolicyHandler" or "AddTransientHttpErrorPolicy" }
                && model.GetSymbolInfo(i).Symbol is IMethodSymbol { ContainingAssembly.Name: PolicyHttpAssembly }),
        ];

        // A private method that only builds a handler's policy goes once its calls are inline (row 6, slice 2).
        IEnumerable<MethodDeclarationSyntax> methods = handlers
            .Select(h => PolicyMethod(h.ArgumentList.Arguments.FirstOrDefault()?.Expression, model))
            .OfType<MethodDeclarationSyntax>()
            .Distinct();
        return [.. handlers, .. methods];
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => node switch
    {
        MethodDeclarationSyntax method => RewriteMethod(method, model),
        _ => RewriteHandler((InvocationExpressionSyntax)node, model),
    };

    // Removed only when every call of the method is a handler this pattern rewrites; otherwise each Manual reason applies here too.
    private static PatternResult RewriteMethod(MethodDeclarationSyntax method, SemanticModel model)
    {
        if (CallsOf(method, model) is not { Count: > 0 } calls)
        {
            return PatternResult.Manual(PollyReason.HandlerPolicyNotInline,
                $"the policy method '{method.Identifier.ValueText}' is also used outside AddPolicyHandler(...); it stays.");
        }

        foreach (InvocationExpressionSyntax handler in calls)
        {
            if (RewriteHandler(handler, model) is ManualResult manual)
            {
                return manual;
            }
        }

        return PatternResult.Fixed([]);
    }

    private static PatternResult RewriteHandler(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var access = (MemberAccessExpressionSyntax)invocation.Expression;
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { Parameters: [IParameterSymbol parameter] } method
            || invocation.ArgumentList.Arguments.Count != 1
            || parameter.Type is not INamedTypeSymbol type
            || (method.Name == "AddPolicyHandler"
                ? type.Name != "IAsyncPolicy"
                : type is not { Name: "Func", TypeArguments: [{ Name: "PolicyBuilder" }, _] }))
        {
            return PatternResult.Manual(PollyReason.HandlerOverloadNotSupported,
                "only AddPolicyHandler(policy) and AddTransientHttpErrorPolicy(p => ...) are rewritten; a request-based selector or a registry is Manual.");
        }

        if (OtherHandler(invocation) is string other)
        {
            return PatternResult.Manual(PollyReason.HandlerOrderNotProvable,
                $"the client also has {other}; the order of several handlers is not proven the same after a rewrite.");
        }

        ExpressionSyntax argument = invocation.ArgumentList.Arguments[0].Expression;
        InvocationExpressionSyntax? creation = method.Name == "AddPolicyHandler"
            ? argument as InvocationExpressionSyntax
            : argument is SimpleLambdaExpressionSyntax { Modifiers.Count: 0, ExpressionBody: InvocationExpressionSyntax body } ? body : null;
        if (creation is not null && !PollyRetry.IsRetryCreation(creation, model) && method.Name == "AddPolicyHandler")
        {
            MethodDeclarationSyntax? policyMethod = PolicyMethod(creation, model);
            if (policyMethod is not null && CallsOf(policyMethod, model) is null)
            {
                return PatternResult.Manual(PollyReason.HandlerPolicyNotInline,
                    $"the policy method '{policyMethod.Identifier.ValueText}' is also used outside AddPolicyHandler(...); it stays.");
            }

            creation = policyMethod is null ? null : Returned(policyMethod);
        }

        if (creation is null || !PollyRetry.IsRetryCreation(creation, model))
        {
            return PatternResult.Manual(PollyReason.HandlerPolicyNotInline,
                "the handler's policy is not a retry call written in place or returned by a private parameterless method of this type (a field or registry policy is Manual so far).");
        }

        if (PollyRetry.ResultType(creation, model) is null
            || PollyRetry.FreshName(invocation, model, BuilderName) is not string builder)
        {
            return PatternResult.Manual(PollyReason.HandlerPolicyNotInline, "the handler's policy is not a PolicyBuilder<HttpResponseMessage> chain.");
        }

        PollyPlan options = PollyRetry.PlanOptions(creation, model, PollyRetry.Indent(access.Name), invocation.Contains(creation) ? null : invocation);
        if (options.Text is not string optionsText)
        {
            return PatternResult.Manual(options.Code ?? PollyReason.PolicyNotLocal, options.Reason ?? "No rewrite.");
        }

        InvocationExpressionSyntax rewritten = invocation
            .WithExpression(access.WithName(SyntaxFactory.IdentifierName("AddResilienceHandler").WithTriviaFrom(access.Name)))
            .WithArgumentList(SyntaxFactory.ParseArgumentList($"(\"{HandlerName}\", {builder} => {builder}.AddRetry({optionsText}))"));
        return PatternResult.Fixed(rewritten, s_namespaces);
    }

    // `M()` calling a private, parameterless, non-generic method of a type declared in one place, in the same file, whose body only
    // returns one expression.
    private static MethodDeclarationSyntax? PolicyMethod(ExpressionSyntax? argument, SemanticModel model) =>
        argument is InvocationExpressionSyntax { Expression: IdentifierNameSyntax, ArgumentList.Arguments.Count: 0 } call
        && model.GetSymbolInfo(call).Symbol is IMethodSymbol { DeclaredAccessibility: Accessibility.Private, Parameters.Length: 0, IsGenericMethod: false } symbol
        && symbol.ContainingType.DeclaringSyntaxReferences.Length == 1
        && symbol.DeclaringSyntaxReferences.SingleOrDefault()?.GetSyntax() is MethodDeclarationSyntax declaration
        && declaration.SyntaxTree == call.SyntaxTree
        && Returned(declaration) is not null
            ? declaration
            : null;

    private static InvocationExpressionSyntax? Returned(MethodDeclarationSyntax method) => method switch
    {
        { ExpressionBody.Expression: InvocationExpressionSyntax value } => value,
        { Body.Statements: [ReturnStatementSyntax { Expression: InvocationExpressionSyntax value }] } => value,
        _ => null,
    };

    // The AddPolicyHandler(M()) calls of the method; null when it is referenced in any other way.
    private static List<InvocationExpressionSyntax>? CallsOf(MethodDeclarationSyntax method, SemanticModel model)
    {
        if (model.GetDeclaredSymbol(method) is not IMethodSymbol symbol || symbol.ContainingType.DeclaringSyntaxReferences is not [SyntaxReference type])
        {
            return null;
        }

        var handlers = new List<InvocationExpressionSyntax>();
        foreach (IdentifierNameSyntax use in type.GetSyntax().DescendantNodes().OfType<IdentifierNameSyntax>()
            .Where(n => n.Identifier.ValueText == symbol.Name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, symbol)))
        {
            if (use.Parent is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0, Parent: ArgumentSyntax { Parent.Parent: InvocationExpressionSyntax handler } }
                || handler.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddPolicyHandler" })
            {
                return null;
            }

            handlers.Add(handler);
        }

        return handlers;
    }

    // Another handler call on the same builder chain, before or after this one.
    private static string? OtherHandler(InvocationExpressionSyntax invocation)
    {
        for (ExpressionSyntax? current = ((MemberAccessExpressionSyntax)invocation.Expression).Expression;
            current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access };
            current = access.Expression)
        {
            if (s_handlerMethods.Contains(access.Name.Identifier.ValueText))
            {
                return access.Name.Identifier.ValueText;
            }
        }

        for (SyntaxNode? parent = invocation.Parent;
            parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax outer } next;
            parent = outer.Parent)
        {
            if (s_handlerMethods.Contains(next.Name.Identifier.ValueText))
            {
                return next.Name.Identifier.ValueText;
            }
        }

        return null;
    }
}
