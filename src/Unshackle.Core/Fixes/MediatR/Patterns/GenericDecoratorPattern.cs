using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

/// <summary>
/// An open generic handler decorator (kgrzybek: <c>UnitOfWorkCommandHandlerDecorator&lt;T&gt; : ICommandHandler&lt;T&gt;</c> with an
/// <c>ICommandHandler&lt;T&gt; decorated</c> parameter, registered with Autofac's <c>RegisterGenericDecorator</c>) stays a container decorator:
/// Mediator resolves handlers through DI (<c>IRequestHandler&lt;TRequest, TResponse&gt;</c>), so the container wraps them as it did for MediatR
/// (measured on 3.0.2, TAAK-193). The generator skips such a class with warning MSG0003, which the fix keeps from being an error.
/// Mediator resolves a void handler as <c>IRequestHandler&lt;T, Unit&gt;</c>, so a void decorator decorates that service: its decorated
/// parameter and the registration's service type follow. A notification decorator would be registered by the generator as a handler of every
/// notification; that is only undone when MediatR is registered by hand in Autofac (<see cref="AutofacMediatorModulePattern"/>).
/// </summary>
public class GenericDecoratorPattern : ISolutionPattern
{
    private const string RegisterDecorator = "RegisterGenericDecorator";

    // Marks a use that is a Scrutor Decorate(typeof(S<>), typeof(D<>)) call (UnregisteredUseAsync).
    private const string ScrutorPrefix = "scrutor:";

    public string Id => "mediatr.generic-decorator";

    public string Description =>
        "Open generic handler decorator D<T> : IRequestHandler<T> (Autofac RegisterGenericDecorator) → decorated IRequestHandler<T, Unit> and typeof(IRequestHandler<,>) as the decorated service; "
        + "a notification decorator stays when MediatR is registered by hand in Autofac";

    private enum Kind
    {
        Void,
        Result,
        Notification,
    }

    /// <summary>True for an open generic class that decorates a MediatR handler of its own type parameter (a constructor parameter of that handler type).</summary>
    public static bool IsDecorator(INamedTypeSymbol type) => Decorated(type) is not null;

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model)
    {
        foreach (SyntaxNode node in root.DescendantNodes())
        {
            switch (node)
            {
                // A decorator of a handler with a response needs nothing beyond the Task → ValueTask of the handler patterns.
                case TypeDeclarationSyntax type when model.GetDeclaredSymbol(type) is INamedTypeSymbol symbol && Decorated(symbol) is { Kind: not Kind.Result }:
                    yield return type;
                    break;
                case TypeOfExpressionSyntax typeOf when VoidService(typeOf, model):
                    yield return typeOf;
                    break;
            }
        }
    }

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model) =>
        PatternResult.Manual("A generic decorator needs the rest of the solution to see how it is registered; run the fix on the solution.");

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        if (node is TypeOfExpressionSyntax)
        {
            return PatternResult.Fixed(SyntaxFactory.ParseExpression("typeof(IRequestHandler<,>)"), "Mediator");
        }

        var type = (TypeDeclarationSyntax)node;
        var symbol = (INamedTypeSymbol)model.GetDeclaredSymbol(type, cancellationToken)!;
        (Kind kind, INamedTypeSymbol decorated) = Decorated(symbol)!.Value;
        if (await UnregisteredUseAsync(symbol, solution, cancellationToken) is string use)
        {
            // Council round 3 (fullcart): name the Scrutor registration itself, not Autofac's, when that is what wraps the handlers.
            if (use.StartsWith(ScrutorPrefix, StringComparison.Ordinal))
            {
                return PatternResult.Manual(
                    $"{symbol.Name} is registered with {use[ScrutorPrefix.Length..]}; under Mediator's source generator the decorator would become a plain handler "
                    + "(a silent behaviour change), and only Autofac's RegisterGenericDecorator is known to wrap Mediator's handlers the same way. Register the decorator by hand.");
            }

            return PatternResult.Manual(
                $"{symbol.Name} is used other than in Autofac's RegisterGenericDecorator(typeof({symbol.Name}<>), ...) ({use}); only that registration is known to wrap Mediator's handlers the same way. Register the decorator by hand.");
        }

        if (kind == Kind.Notification)
        {
            return await RegistersMediatRInAutofacAsync(solution, cancellationToken)
                ? PatternResult.Fixed(type)
                : PatternResult.Manual(
                    $"Mediator's source generator registers the open generic {symbol.Name}<T> as a handler of every notification, so it would decorate itself; "
                    + "MediatR is not registered by hand in Autofac here (where the fix leaves Mediator's handler registrations out). Move the decoration into a custom INotificationPublisher by hand.");
        }

        return RewriteVoid(type, symbol, decorated, model);
    }

    // The class and the handler type of its decorated constructor parameter; null when it is no decorator.
    private static (Kind Kind, INamedTypeSymbol Decorated)? Decorated(INamedTypeSymbol type)
    {
        if (type is not { TypeKind: TypeKind.Class, IsAbstract: false, IsGenericType: true } || !SymbolEqualityComparer.Default.Equals(type, type.OriginalDefinition)
            || !type.Locations.Any(l => l.IsInSource))
        {
            return null;
        }

        foreach (INamedTypeSymbol handler in type.AllInterfaces)
        {
            Kind? kind = handler.OriginalDefinition.MetadataName switch
            {
                "IRequestHandler`1" when MediatRSyntax.IsMediatR(handler, "IRequestHandler") => Kind.Void,
                "IRequestHandler`2" when MediatRSyntax.IsMediatR(handler, "IRequestHandler") => Kind.Result,
                "INotificationHandler`1" when MediatRSyntax.IsMediatR(handler, "INotificationHandler") => Kind.Notification,
                _ => null,
            };
            if (kind is null || handler.TypeArguments[0] is not ITypeParameterSymbol parameter || !SymbolEqualityComparer.Default.Equals(parameter.DeclaringType, type))
            {
                continue;
            }

            INamedTypeSymbol? decorated = type.InstanceConstructors.SelectMany(c => c.Parameters).Select(p => p.Type).OfType<INamedTypeSymbol>()
                .FirstOrDefault(p => p.TypeKind == TypeKind.Interface
                    && (SymbolEqualityComparer.Default.Equals(p, handler) || p.AllInterfaces.Contains(handler, SymbolEqualityComparer.Default))
                    && type.AllInterfaces.Contains(p, SymbolEqualityComparer.Default))
                // Already rewritten in this file (an earlier pattern run sees MediatR's IRequestHandler<T, Unit>, which MediatR does not relate to IRequestHandler<T>).
                ?? (kind == Kind.Void ? type.InstanceConstructors.SelectMany(c => c.Parameters).Select(p => p.Type).OfType<INamedTypeSymbol>()
                    .FirstOrDefault(p => IsRewrittenVoid(p, parameter)) : null);
            if (decorated is not null)
            {
                return (kind.Value, decorated);
            }
        }

        return null;
    }

    // IRequestHandler<T, Unit> as RewriteVoid writes it. In a file without "using MediatR" (kgrzybek's module decorators only see their own
    // ICommandHandler<T> marker) it does not bind until the fix adds "using Mediator", so it is an error type of that name (TAAK-210).
    private static bool IsRewrittenVoid(INamedTypeSymbol type, ITypeParameterSymbol parameter) =>
        type is { Name: "IRequestHandler", Arity: 2 }
        && SymbolEqualityComparer.Default.Equals(type.TypeArguments[0], parameter)
        && (type.TypeKind == TypeKind.Error
            ? type.TypeArguments[1] is { Name: "Unit" }
            : MediatRSyntax.IsMediatR(type.OriginalDefinition, "IRequestHandler") && type.TypeArguments[1] is INamedTypeSymbol unit && MediatRSyntax.IsMediatR(unit, "Unit"));

    // typeof(S<>) as the service of RegisterGenericDecorator(typeof(D<>), typeof(S<>)) with D a void decorator and S a handler interface of one type parameter.
    private static bool VoidService(TypeOfExpressionSyntax typeOf, SemanticModel model) =>
        typeOf.Parent is ArgumentSyntax { Parent: ArgumentListSyntax { Arguments: [{ Expression: TypeOfExpressionSyntax decorator }, var service, ..], Parent: InvocationExpressionSyntax invocation } }
        && service.Expression == typeOf
        && IsRegisterDecorator(invocation, model)
        && model.GetTypeInfo(decorator.Type).Type is INamedTypeSymbol { IsUnboundGenericType: true } unbound
        && Decorated(unbound.OriginalDefinition) is { Kind: Kind.Void }
        && model.GetTypeInfo(typeOf.Type).Type is INamedTypeSymbol { IsUnboundGenericType: true, Arity: 1 } serviceType
        && (MediatRSyntax.IsMediatR(serviceType.OriginalDefinition, "IRequestHandler")
            || serviceType.OriginalDefinition.AllInterfaces.Any(i => i.OriginalDefinition.MetadataName == "IRequestHandler`1" && MediatRSyntax.IsMediatR(i, "IRequestHandler")));

    private static bool IsRegisterDecorator(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: RegisterDecorator }
        && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method
        && method.ContainingNamespace.ToDisplayString().StartsWith("Autofac", StringComparison.Ordinal);

    // Every reference outside the decorator itself must be typeof(D<>) as the first argument of RegisterGenericDecorator; a Scrutor Decorate use
    // (marked with ScrutorPrefix) wherever it is, else the first other use, or null.
    private static async Task<string?> UnregisteredUseAsync(INamedTypeSymbol decorator, Solution solution, CancellationToken cancellationToken)
    {
        string? first = null;
        foreach (ReferencedSymbol referenced in await SymbolFinder.FindReferencesAsync(decorator, solution, cancellationToken))
        {
            foreach (ReferenceLocation location in referenced.Locations)
            {
                SyntaxNode root = (await location.Document.GetSyntaxRootAsync(cancellationToken))!;
                SyntaxNode name = root.FindNode(location.Location.SourceSpan, getInnermostNodeForTie: true);
                if (decorator.DeclaringSyntaxReferences.Any(r => r.SyntaxTree == name.SyntaxTree && r.Span.Contains(name.Span)))
                {
                    continue;
                }

                SemanticModel model = (await location.Document.GetSemanticModelAsync(cancellationToken))!;
                if (name.FirstAncestorOrSelf<TypeOfExpressionSyntax>() is not { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax invocation } list } argument }
                    || list.Arguments.IndexOf(argument) != 0
                    || !IsRegisterDecorator(invocation, model))
                {
                    string at = $"{location.Document.Name}:{location.Location.GetLineSpan().StartLinePosition.Line + 1}";

                    // A Scrutor Decorate call is the registration the report should name, wherever it is among the uses (fullcart).
                    if (name.FirstAncestorOrSelf<TypeOfExpressionSyntax>() is { Parent: ArgumentSyntax { Parent: ArgumentListSyntax { Parent: InvocationExpressionSyntax scrutor } } }
                        && scrutor.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Decorate" } decorate
                        && IsScrutor(scrutor, model))
                    {
                        return $"{ScrutorPrefix}Scrutor's {decorate.Name}{scrutor.ArgumentList} at {at}";
                    }

                    first ??= at;
                }
            }
        }

        return first;
    }

    // Council round 4 (Q4): Decorate must resolve to a method of Scrutor; only an unresolved call falls back to the syntax match, so a user's own
    // Decorate does not cause a false partial. Scrutor declares Decorate in Microsoft.Extensions.DependencyInjection, so its assembly counts too.
    private static bool IsScrutor(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        SymbolInfo info = model.GetSymbolInfo(invocation);
        if ((info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is not IMethodSymbol method)
        {
            return true;
        }

        string ns = method.ContainingNamespace.ToDisplayString();
        return method.ContainingAssembly?.Name == "Scrutor" || ns == "Scrutor" || ns.StartsWith("Scrutor.", StringComparison.Ordinal);
    }

    // builder.RegisterAssemblyTypes(typeof(<a MediatR type>).Assembly ...): MediatR registered by hand in Autofac (TAAK-196).
    private static async Task<bool> RegistersMediatRInAutofacAsync(Solution solution, CancellationToken cancellationToken)
    {
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            SyntaxNode root = (await document.GetSyntaxRootAsync(cancellationToken))!;
            // In the file the module itself is in, AutofacMediatorModulePattern ran first: its Populate filters on "Mediator.Internals".
            if (root.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Populate" }
                && i.ArgumentList.DescendantNodes().OfType<LiteralExpressionSyntax>().Any(l => l.Token.ValueText == "Mediator.Internals")))
            {
                return true;
            }

            List<InvocationExpressionSyntax> scans =
            [
                .. root.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(i => i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "RegisterAssemblyTypes" }),
            ];
            if (scans.Count == 0)
            {
                continue;
            }

            SemanticModel model = (await document.GetSemanticModelAsync(cancellationToken))!;
            if (scans.Any(s => s.ArgumentList.DescendantNodes().OfType<TypeOfExpressionSyntax>()
                .Any(t => model.GetTypeInfo(t.Type).Type?.ContainingAssembly?.Name == "MediatR")))
            {
                return true;
            }
        }

        return false;
    }

    // ICommandHandler<T> decorated → IRequestHandler<T, Unit> decorated, in fields, parameters and locals; the base list keeps ICommandHandler<T>.
    private static PatternResult RewriteVoid(TypeDeclarationSyntax type, INamedTypeSymbol symbol, INamedTypeSymbol decorated, SemanticModel model)
    {
        if (decorated.ContainingAssembly.Name != "MediatR" && decorated.GetMembers().Length > 0)
        {
            return PatternResult.Manual($"{decorated.Name} declares members of its own; the decorated handler becomes IRequestHandler<T, Unit>, which does not have them. Rewrite {symbol.Name} by hand.");
        }

        List<TypeSyntax> written =
        [
            .. type.DescendantNodes().OfType<TypeSyntax>()
                .Where(t => t is GenericNameSyntax or QualifiedNameSyntax { Right: GenericNameSyntax } or AliasQualifiedNameSyntax { Name: GenericNameSyntax })
                .Where(t => t.Parent is not QualifiedNameSyntax and not AliasQualifiedNameSyntax && t.FirstAncestorOrSelf<BaseListSyntax>() is null)
                .Where(t => SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(t).Type, decorated)),
        ];

        // Only stored, assigned and called: anything else may need the old interface.
        var holders = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (TypeSyntax t in written)
        {
            SyntaxNode? declared = t.Parent switch
            {
                ParameterSyntax parameter => parameter,
                VariableDeclarationSyntax variable => variable.Variables.Count == 1 ? variable.Variables[0] : null,
                _ => null,
            };
            if (declared is null || model.GetDeclaredSymbol(declared) is not ISymbol holder)
            {
                return PatternResult.Manual($"{decorated.ToDisplayString()} is written in {symbol.Name} other than as a field, parameter or local; rewrite the decorator by hand.");
            }

            holders.Add(holder);
        }

        foreach (IdentifierNameSyntax use in type.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (model.GetSymbolInfo(use).Symbol is not ISymbol used || !holders.Contains(used))
            {
                continue;
            }

            SyntaxNode expression = use.Parent is MemberAccessExpressionSyntax { Name: var right } thisAccess && right == use && thisAccess.Expression is ThisExpressionSyntax ? thisAccess : use;
            bool called = expression.Parent is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Handle" } member && member.Expression == expression
                && member.Parent is InvocationExpressionSyntax;
            bool assigned = expression.Parent is AssignmentExpressionSyntax assignment
                && (assignment.Left == expression || holders.Contains(model.GetSymbolInfo(assignment.Left).Symbol!));
            if (!called && !assigned)
            {
                return PatternResult.Manual($"The decorated handler {use.Identifier.ValueText} is used other than to call Handle; rewrite {symbol.Name} by hand.");
            }
        }

        string request = decorated.TypeArguments[0].Name;
        TypeDeclarationSyntax rewritten = type.ReplaceNodes(written, (original, _) =>
            SyntaxFactory.ParseTypeName($"IRequestHandler<{request}, Unit>").WithTriviaFrom(original));
        return PatternResult.Fixed(rewritten, "Mediator");
    }
}
