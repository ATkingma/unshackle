using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

public class RequestProcessorPattern : IPattern
{
    private const string PreProcessor = "IRequestPreProcessor`1";
    private const string PostProcessor = "IRequestPostProcessor`2";

    public string Id => "mediatr.request-processor";

    public string Description =>
        "IRequestPreProcessor<TReq> / IRequestPostProcessor<TReq, TRes>: Task Process(...) → MessagePreProcessor<TReq, TRes> / MessagePostProcessor<TReq, TRes> with protected override ValueTask Handle(...)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c =>
            model.GetDeclaredSymbol(c) is { } symbol
            && (MediatRSyntax.DeclaredInterface(symbol, PreProcessor) ?? MediatRSyntax.DeclaredInterface(symbol, PostProcessor)) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var type = (ClassDeclarationSyntax)node;
        INamedTypeSymbol symbol = model.GetDeclaredSymbol(type)!;
        INamedTypeSymbol? pre = MediatRSyntax.DeclaredInterface(symbol, PreProcessor);
        INamedTypeSymbol? post = MediatRSyntax.DeclaredInterface(symbol, PostProcessor);
        if ((pre is not null && post is not null) || symbol.IsAbstract || symbol.IsStatic)
        {
            return PatternResult.Manual("Abstract or both pre- and post-processor: Mediator needs one MessagePreProcessor or MessagePostProcessor class per kind; rewrite it by hand.");
        }

        INamedTypeSymbol processor = (pre ?? post)!;
        MethodDeclarationSyntax? method = symbol.FindImplementationForInterfaceMember(processor.GetMembers("Process")[0])?.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax()).OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Parent == type);
        if (method is null)
        {
            return PatternResult.Manual("Process is not declared in this class; rewrite the processor by hand.");
        }

        if (MediatRSyntax.RejectExplicit(method, "rewrite the processor by hand.") is { } explicitProcess)
        {
            return explicitProcess;
        }

        MethodDeclarationSyntax? body = method.ReturnType is IdentifierNameSyntax { Identifier.ValueText: "Task" } or QualifiedNameSyntax { Right.Identifier.ValueText: "Task" }
            ? MediatRSyntax.WithCompletedValueTask(method, model)
            : null;
        if (body is null)
        {
            return PatternResult.Manual("Process returns something that is not a Task; Mediator needs a ValueTask, convert it by hand (e.g. make the method async).");
        }

        ITypeSymbol message = processor.TypeArguments[0];
        ClassDeclarationSyntax result = type;
        TypeSyntax response;
        if (post is not null)
        {
            response = SyntaxFactory.ParseTypeName(post.TypeArguments[1].ToMinimalDisplayString(model, type.SpanStart));
        }
        else if (message is ITypeParameterSymbol)
        {
            // Mediator's pre-processor knows the response type: the class gets a second type parameter (<> → <,> in typeof).
            if (symbol.TypeParameters.Any(p => p.Name == "TResponse"))
            {
                return PatternResult.Manual("The pre-processor gets a second type parameter TResponse, but that name is taken; rewrite it by hand.");
            }

            response = SyntaxFactory.IdentifierName("TResponse");
            result = result.WithTypeParameterList(result.TypeParameterList!.AddParameters(SyntaxFactory.TypeParameter("TResponse")));
            result = result.WithTypeParameterList(result.TypeParameterList!.WithParameters(MediatRSyntax.CommaList([.. result.TypeParameterList.Parameters.Select(p => p.WithoutTrivia())])));
        }
        else if (MediatRSyntax.ResponseOf(message, model, type.SpanStart) is string closed)
        {
            response = SyntaxFactory.ParseTypeName(closed);
        }
        else
        {
            return PatternResult.Manual("The response type of the pre-processed request is unknown (not IRequest or IRequest<T>); rewrite the processor by hand.");
        }

        TypeSyntax messageType = SyntaxFactory.ParseTypeName(message.ToMinimalDisplayString(model, type.SpanStart));
        GenericNameSyntax baseClass = SyntaxFactory.GenericName(SyntaxFactory.Identifier(pre is not null ? "MessagePreProcessor" : "MessagePostProcessor"))
            .WithTypeArgumentList(SyntaxFactory.TypeArgumentList(MediatRSyntax.CommaList([messageType, response])));
        result = result.ReplaceNode(result.Members[type.Members.IndexOf(method)], MediatRSyntax.AsHandleOverride(body, SyntaxFactory.IdentifierName("ValueTask")));
        if (MediatRSyntax.WithBaseClass(type, result, processor, baseClass, model) is not ClassDeclarationSyntax withBase)
        {
            return PatternResult.Manual("The processor already has a base class; Mediator's MessagePreProcessor/MessagePostProcessor must be it. Rewrite it by hand.");
        }

        return PatternResult.Fixed(
            MediatRSyntax.WithConstraint(withBase, message, "IMessage", t => MediatRSyntax.IsMediatR(t, "IBaseRequest", "INotification")),
            "System.Threading.Tasks");
    }
}
