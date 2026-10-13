using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

/// <summary>
/// Code that uses a request processor <see cref="RequestProcessorPattern"/> rewrites: <c>new P&lt;TReq&gt;()</c> → <c>new P&lt;TReq, TRes&gt;()</c> (a pre-processor over an open
/// request type gets a response type parameter) and <c>await p.Process(req, ct)</c> → <c>await p.Handle(req, next, ct)</c> (the public pipeline Handle; the protected one is the override).
/// </summary>
public class RequestProcessorCallerPattern : IPattern
{
    private const string PreProcessor = "IRequestPreProcessor`1";
    private const string PostProcessor = "IRequestPostProcessor`2";

    public string Id => "mediatr.request-processor-caller";

    public string Description =>
        "Use of a rewritten request processor (tests): P<TReq> → P<TReq, TRes>, await p.Process(req, ct) → await p.Handle(req, (_, _) => default, ct); anything else → Manual with the line";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            switch (node)
            {
                case GenericNameSyntax generic when IsArityChanged(generic, model) && !IsInsideAddMediatR(generic):
                    yield return generic;
                    break;
                case IdentifierNameSyntax { Identifier.ValueText: "Process" } name when ProcessMethod(name, model) is not null && !IsInsideAddMediatR(name):
                    yield return name;
                    if (name.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax call } access && access.Name == name && call.Expression == access)
                    {
                        yield return call.ArgumentList;
                    }

                    break;
            }
        }
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        switch (node)
        {
            case GenericNameSyntax generic:
                return RewriteReference(generic, model);
            case IdentifierNameSyntax name:
                return Analyze(name, model) is { Problem: { } problem }
                    ? PatternResult.Manual(problem)
                    : PatternResult.Fixed(SyntaxFactory.IdentifierName("Handle"));
            case ArgumentListSyntax arguments:
                return RewriteArguments(arguments, model);
            default:
                return PatternResult.Manual("Not a processor reference.");
        }
    }

    private static PatternResult RewriteReference(GenericNameSyntax generic, SemanticModel model)
    {
        if (generic.TypeArgumentList.Arguments is not [var argument] || argument is OmittedTypeArgumentSyntax)
        {
            return PatternResult.Manual($"{generic.ToString()} at line {Line(generic)} is an open generic processor outside AddMediatR; Mediator's processor has a second type parameter (TResponse), rewrite it by hand.");
        }

        ITypeSymbol? request = model.GetTypeInfo(argument).Type;
        string? response = request is null ? null : MediatRSyntax.ResponseOf(request, model, generic.SpanStart);
        if (response is null)
        {
            return PatternResult.Manual($"{generic.ToString()} at line {Line(generic)}: the response type of {argument.ToString()} is unknown (not IRequest or IRequest<T>); Mediator's processor needs it as second type argument, rewrite it by hand.");
        }

        return PatternResult.Fixed(generic.WithTypeArgumentList(generic.TypeArgumentList.WithArguments(
            MediatRSyntax.CommaList([argument.WithoutTrivia(), SyntaxFactory.ParseTypeName(response)]))));
    }

    private static PatternResult RewriteArguments(ArgumentListSyntax arguments, SemanticModel model)
    {
        var name = (IdentifierNameSyntax)((MemberAccessExpressionSyntax)((InvocationExpressionSyntax)arguments.Parent!).Expression).Name;
        if (Analyze(name, model) is not { } analysis)
        {
            return PatternResult.Manual("Not a processor call.");
        }

        if (analysis.Problem is not null)
        {
            return PatternResult.Manual(analysis.Problem);
        }

        string[] values = [.. arguments.Arguments.Select(a => a.Expression.ToString())];
        string next = analysis.IsPre
            ? "static (_, _) => default"
            : $"(_, _) => new ValueTask<{analysis.ResponseType}>({values[1]})";
        string[] list = analysis.IsPre ? [values[0], next, values[1]] : [values[0], next, values[2]];
        return PatternResult.Fixed(SyntaxFactory.ParseArgumentList($"({string.Join(", ", list)})"), "System.Threading.Tasks");
    }

    // The Process method of a processor class that RequestProcessorPattern rewrites, or of the MediatR interface itself.
    private static IMethodSymbol? ProcessMethod(IdentifierNameSyntax name, SemanticModel model)
    {
        if (model.GetSymbolInfo(name).Symbol is not IMethodSymbol { Name: "Process" } method || method.ContainingType is not { } owner)
        {
            return null;
        }

        bool mediatR = MediatRSyntax.IsMediatR(owner, "IRequestPreProcessor", "IRequestPostProcessor");
        return mediatR || IsProcessorClass(owner) ? method : null;
    }

    private static bool IsProcessorClass(INamedTypeSymbol type) =>
        type.TypeKind == TypeKind.Class
        && type.OriginalDefinition is { } definition
        && definition.Locations.Any(l => l.IsInSource)
        && (MediatRSyntax.DeclaredInterface(definition, PreProcessor) ?? MediatRSyntax.DeclaredInterface(definition, PostProcessor)) is not null;

    // A reference to a pre-processor class with an open message type: that class gets a second type parameter.
    private static bool IsArityChanged(GenericNameSyntax generic, SemanticModel model) =>
        generic.Identifier.ValueText.Length > 0
        && model.GetSymbolInfo(generic).Symbol is INamedTypeSymbol { TypeKind: TypeKind.Class } type
        && IsProcessorClass(type)
        && MediatRSyntax.DeclaredInterface(type.OriginalDefinition, PreProcessor) is { TypeArguments: [ITypeParameterSymbol] }
        && !IsInsideDeclaration(generic, type.OriginalDefinition, model);

    private static bool IsInsideDeclaration(SyntaxNode node, INamedTypeSymbol type, SemanticModel model) =>
        node.Ancestors().OfType<ClassDeclarationSyntax>().Any(c => SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(c), type));

    // The lambda of AddMediatR (and the registrations in it): AddMediatRPattern rewrites those.
    private static bool IsInsideAddMediatR(SyntaxNode node) =>
        node.Ancestors().OfType<InvocationExpressionSyntax>().Any(i =>
            i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "AddMediatR" } or IdentifierNameSyntax { Identifier.ValueText: "AddMediatR" });

    private static CallAnalysis? Analyze(IdentifierNameSyntax name, SemanticModel model)
    {
        IMethodSymbol? method = ProcessMethod(name, model);
        if (method is null)
        {
            return null;
        }

        int line = Line(name);
        if (MediatRSyntax.IsMediatR(method.ContainingType, "IRequestPreProcessor", "IRequestPostProcessor"))
        {
            return new CallAnalysis($"Process at line {line} is called through the MediatR interface, which is gone after the migration; call the processor class's Handle by hand.");
        }

        bool isPre = MediatRSyntax.DeclaredInterface(method.ContainingType.OriginalDefinition, PreProcessor) is not null;
        if (name.Parent is not MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax { Parent: AwaitExpressionSyntax { Parent: ExpressionStatementSyntax } } call } access
            || access.Name != name || call.Expression != access)
        {
            return new CallAnalysis($"Process at line {line} is not a plain `await x.Process(...);` statement (result used, not awaited, mocked or a method group); Mediator's Handle takes a next delegate and returns ValueTask<TResponse>, rewrite it by hand.");
        }

        if (call.ArgumentList.Arguments.Count != method.Parameters.Length || call.ArgumentList.Arguments.Any(a => a.NameColon is not null || a.RefKindKeyword.ValueText.Length > 0))
        {
            return new CallAnalysis($"Process at line {line} has named or ref arguments; rewrite it by hand.");
        }

        string response = isPre ? string.Empty : method.Parameters[1].Type.ToMinimalDisplayString(model, call.SpanStart);
        return new CallAnalysis(null, isPre, response);
    }

    private static int Line(SyntaxNode node) => node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;

    private record CallAnalysis(string? Problem, bool IsPre = false, string ResponseType = "");
}
