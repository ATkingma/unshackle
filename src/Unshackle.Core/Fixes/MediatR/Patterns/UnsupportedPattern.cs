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
        ["IRequestPreProcessor`1"] =
            "IRequestPreProcessor has no Mediator twin; derive from Mediator.MessagePreProcessor<TMessage, TResponse>, override Handle(message, cancellationToken) returning ValueTask, and add it to PipelineBehaviors in AddMediator.",
        ["IRequestPostProcessor`2"] =
            "IRequestPostProcessor has no Mediator twin; derive from Mediator.MessagePostProcessor<TMessage, TResponse>, override Handle(message, response, cancellationToken) returning ValueTask, and add it to PipelineBehaviors in AddMediator.",
        ["IRequestExceptionHandler`3"] =
            "IRequestExceptionHandler has no Mediator twin; derive from Mediator.MessageExceptionHandler<TMessage, TResponse, TException>, return an ExceptionHandlingResult from Handle, and add it to PipelineBehaviors in AddMediator.",
        ["IRequestExceptionAction`2"] =
            "IRequestExceptionAction has no Mediator twin; derive from Mediator.MessageExceptionHandler<TMessage, TResponse, TException>, run the action and return NotHandled, and add it to PipelineBehaviors in AddMediator.",
        ["INotificationPublisher"] =
            "Mediator.INotificationPublisher has a different Publish(NotificationHandlers<T>, T, CancellationToken) returning ValueTask; rewrite the publisher and set NotificationPublisherType in AddMediator.",
        ["IStreamPipelineBehavior`2"] =
            "Mediator.IStreamPipelineBehavior<TMessage, TResponse> needs where TMessage : IStreamMessage and calls next(message, cancellationToken) (StreamHandlerDelegate<TMessage, TResponse>); rewrite Handle and add it to StreamPipelineBehaviors in AddMediator.",
    };

    public string Id => "mediatr.unsupported";

    public string Description => "MediatR pre/post-processors, exception handlers/actions, notification publishers, stream behaviors and generic messages and request handlers → Manual with advice";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<TypeDeclarationSyntax>().Where(t => Advice(t, model) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual(Advice((TypeDeclarationSyntax)node, model) ?? "No MediatR type without a Mediator twin found.");

    private static string? Advice(TypeDeclarationSyntax type, SemanticModel model)
    {
        if (model.GetDeclaredSymbol(type) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        foreach (INamedTypeSymbol implemented in symbol.Interfaces.Where(i => i.ContainingNamespace.ToDisplayString().StartsWith("MediatR", StringComparison.Ordinal)))
        {
            string name = implemented.OriginalDefinition.MetadataName;
            if (s_advice.TryGetValue(name, out string? advice))
            {
                return advice;
            }

            if (symbol.IsGenericType && s_noGeneric.Contains(name))
            {
                return GenericMessageAdvice;
            }
        }

        return null;
    }
}
