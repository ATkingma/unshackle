using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class BehaviorDiRegistrationPattern : IPattern
{
    public string Id => "mediatr.behavior-di-registration";

    public string Description => "services.AddTransient/AddScoped/AddSingleton(typeof(IPipelineBehavior<,>), typeof(X<,>)) → unchanged; Mediator 3 resolves behaviors from DI, the using swap does the rest";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<InvocationExpressionSyntax>().Where(i =>
            i.ArgumentList.Arguments is [{ Expression: TypeOfExpressionSyntax serviceType }, { Expression: TypeOfExpressionSyntax }]
            && model.GetSymbolInfo(i).Symbol is IMethodSymbol { Name: "AddTransient" or "AddScoped" or "AddSingleton", ContainingNamespace: var ns }
            && ns.ToDisplayString() == "Microsoft.Extensions.DependencyInjection"
            && model.GetTypeInfo(serviceType.Type).Type is INamedTypeSymbol { MetadataName: "IPipelineBehavior`2", ContainingAssembly.Name: "MediatR" });

    // Measured (TAAK-145): the generated Mediator 3.0.2 runs every IPipelineBehavior<,> registered in the container, for each lifetime.
    // The node stays as it is; MediatRFix.SwappedTypes turns IPipelineBehavior<,> into Mediator's.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) => PatternResult.Fixed(node);
}
