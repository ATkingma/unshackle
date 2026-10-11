using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

/// <summary>Reports MediatR types without a 1-on-1 Mediator 3.0.2 twin as Manual, with what to write instead.</summary>
public class UnsupportedPattern : IPattern
{
    private const string GenericMessageAdvice =
        "Mediator's source generator does not support generic messages or request handlers; replace them with one non-generic message and handler per closed type.";

    // Generic types implementing these break the generated code (open type parameters in Mediator.g.cs).
    private static readonly HashSet<string> s_noGeneric =
        new(["IRequest", "IRequest`1", "IStreamRequest`1", "INotification", "IRequestHandler`1", "IRequestHandler`2", "IStreamRequestHandler`2"], StringComparer.Ordinal);

    // Keyed by metadata name; namespace MediatR or MediatR.Pipeline.
    private static readonly Dictionary<string, string> s_advice = new(StringComparer.Ordinal)
    {
        ["IRequestExceptionAction`2"] =
            "IRequestExceptionAction has no Mediator twin; derive from Mediator.MessageExceptionHandler<TMessage, TResponse, TException>, run the action and return NotHandled, and add it to PipelineBehaviors in AddMediator.",
        ["INotificationPublisher"] =
            "Mediator.INotificationPublisher has a different Publish(NotificationHandlers<T>, T, CancellationToken) returning ValueTask; rewrite the publisher and set NotificationPublisherType in AddMediator.",
    };

    public string Id => "mediatr.unsupported";

    public string Description => "MediatR exception actions, notification publishers and generic messages and request handlers → Manual with advice";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<TypeDeclarationSyntax>().Where(t => Advice(t, model) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual(Advice((TypeDeclarationSyntax)node, model) ?? "No MediatR type without a Mediator twin found.");

    private static string? Advice(TypeDeclarationSyntax type, SemanticModel model)
    {
        // A user's own marker interface (IAppQueryHandler<TQuery, TResult> : IRequestHandler<...>) is fine; the generator only reads classes.
        if (model.GetDeclaredSymbol(type) is not INamedTypeSymbol { TypeKind: not TypeKind.Interface } symbol)
        {
            return null;
        }

        // Advice only on the type that declares the interface; a generic message is reported also when it gets IRequest through a base type.
        foreach (INamedTypeSymbol implemented in symbol.Interfaces.Where(IsMediatR))
        {
            if (s_advice.TryGetValue(implemented.OriginalDefinition.MetadataName, out string? advice))
            {
                return advice;
            }
        }

        return symbol.IsGenericType && symbol.AllInterfaces.Any(i => IsMediatR(i) && s_noGeneric.Contains(i.OriginalDefinition.MetadataName))
            ? GenericMessageAdvice
            : null;
    }

    private static bool IsMediatR(INamedTypeSymbol type) => type.ContainingNamespace.ToDisplayString().StartsWith("MediatR", StringComparison.Ordinal);
}
