using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.MediatR.Patterns;

// MediatR registered by hand in Autofac (kgrzybek's MediatorModule, TAAK-196): builder.RegisterAssemblyTypes(typeof(IMediator).Assembly)
// registers MediatR's own classes, the handlers come from the user's own Autofac scans, and RequestPre/PostProcessorBehavior are
// registered as open generics. Mediator's dispatcher is generated code that only AddMediator registers, so the scan becomes
// AddMediator on a ServiceCollection, populated into the builder without Mediator's handler registrations: the handlers stay
// Autofac's (decorators, AllConstructorFinder, contravariance), exactly as they were for MediatR (measured on 3.0.2).
public class AutofacMediatorModulePattern : IPattern
{
    private const string PipelineNamespace = "MediatR.Pipeline";

    public string Id => "mediatr.autofac-mediator-module";

    public string Description =>
        "builder.RegisterAssemblyTypes(typeof(IMediator).Assembly) in an Autofac module → AddMediator on a ServiceCollection, Populate'd without its handler registrations; "
        + "MediatR.Pipeline types leave the scan lists, RegisterGeneric(RequestPre/PostProcessorBehavior) goes";

    // The block (method body) around the scan: its other statements change with it.
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().OfType<ExpressionStatementSyntax>()
            .Where(s => s.Parent is BlockSyntax && ScanOfMediatR(s, model) is not null)
            .Select(s => s.Parent!)
            .Distinct();

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        var block = (BlockSyntax)node;
        List<ExpressionStatementSyntax> scans = [.. block.Statements.OfType<ExpressionStatementSyntax>().Where(s => ScanOfMediatR(s, model) is not null)];
        if (scans is not [ExpressionStatementSyntax scan])
        {
            return PatternResult.Manual("MediatR's assembly is scanned more than once in this method; register Mediator with AddMediator by hand.");
        }

        InvocationExpressionSyntax register = ScanOfMediatR(scan, model)!;
        if (Lifetime(scan, register) is not string lifetime)
        {
            return PatternResult.Manual(
                $"'{scan.Expression}' configures more than AsImplementedInterfaces and a lifetime; register Mediator with AddMediator by hand.");
        }

        Compilation compilation = model.Compilation;
        if (compilation.GetTypeByMetadataName("Autofac.Extensions.DependencyInjection.AutofacRegistration") is null
            || compilation.GetTypeByMetadataName("Microsoft.Extensions.DependencyInjection.ServiceCollection") is null)
        {
            return PatternResult.Manual(
                "Mediator only registers through an IServiceCollection, and this project has no Autofac.Extensions.DependencyInjection to Populate one into the ContainerBuilder; "
                + "add that package or register Mediator by hand.");
        }

        // Statements registering MediatR's pipeline behaviors as open generics: only the processor behaviors may go.
        List<StatementSyntax> dropped = [];
        foreach (ExpressionStatementSyntax statement in block.Statements.OfType<ExpressionStatementSyntax>().Where(s => s != scan))
        {
            if (RegisteredPipelineType(statement, model) is not INamedTypeSymbol behavior)
            {
                continue;
            }

            if (behavior.MetadataName is not ("RequestPreProcessorBehavior`2" or "RequestPostProcessorBehavior`2"))
            {
                return PatternResult.Manual(
                    $"'{statement}' runs MediatR's exception handlers; list them in AddMediator's PipelineBehaviors by hand (Mediator only runs what is listed).");
            }

            if (Processors(compilation, behavior.MetadataName == "RequestPreProcessorBehavior`2" ? "IRequestPreProcessor`1" : "IRequestPostProcessor`2").FirstOrDefault() is INamedTypeSymbol processor)
            {
                return PatternResult.Manual(
                    $"'{statement}' runs request processors such as {processor.Name}; list them in AddMediator's PipelineBehaviors in the order Autofac registered them, by hand.");
            }

            dropped.Add(statement);
        }

        // typeof(MediatR.Pipeline.X<>) in the lists of open types the module scans for: Mediator has no such interfaces.
        // Under MediatR an exception handler found this way never ran (no exception behavior registered), and after the rewrite it is not registered.
        List<ExpressionSyntax> removedTypes =
        [
            .. block.DescendantNodes().OfType<TypeOfExpressionSyntax>().Where(t => IsPipelineType(model.GetTypeInfo(t.Type).Type) && IsListElement(t)),
        ];
        SyntaxNode? left = block.DescendantNodes()
            .Where(n => n is TypeSyntax && !dropped.Any(d => d.Span.Contains(n.Span)) && !removedTypes.Any(t => t.Span.Contains(n.Span)))
            .FirstOrDefault(n => IsPipelineType(model.GetSymbolInfo(n).Symbol as ITypeSymbol));
        if (left is not null)
        {
            return PatternResult.Manual($"'{left}' names a MediatR.Pipeline type that Mediator does not have; migrate this registration by hand.");
        }

        string indentation = string.Concat(scan.GetLeadingTrivia().Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse().Select(t => t.ToFullString()));
        string inner = indentation + "    ";
        string services = FreeName(model, scan.SpanStart, "mediatorServices");
        string options = FreeName(model, scan.SpanStart, "mediatorOptions");
        string descriptor = FreeName(model, scan.SpanStart, "descriptor");
        string builder = ((MemberAccessExpressionSyntax)register.Expression).Expression.ToString();
        string newLine = block.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToFullString() is { Length: > 0 } eol ? eol : "\n";
        List<string> settings = [$"{options}.ServiceLifetime = {lifetime}"];
        // MediatR's assembly scan registers both of its publishers; Autofac's last one, TaskWhenAllPublisher, was the one injected (measured, 12.0-12.5).
        if (compilation.GetTypeByMetadataName("MediatR.NotificationPublishers.TaskWhenAllPublisher") is not null)
        {
            settings.Add($"{options}.NotificationPublisherType = typeof(global::Mediator.TaskWhenAllPublisher)");
        }

        string code =
            $"{indentation}// Mediator's dispatcher; the handlers stay registered by this module, as they were for MediatR.{newLine}"
            + $"{indentation}var {services} = new ServiceCollection();{newLine}"
            + $"{indentation}{services}.AddMediator({options} =>{newLine}"
            + $"{indentation}{{{newLine}"
            + string.Concat(settings.Select(s => $"{inner}{s};{newLine}"))
            + $"{indentation}}});{newLine}"
            + $"{indentation}{builder}.Populate({services}.Where({descriptor} => {descriptor}.ServiceType.Namespace is \"Mediator\" or \"Mediator.Internals\"{newLine}"
            + $"{inner}&& !({descriptor}.ServiceType.IsGenericType && {descriptor}.ServiceType.Assembly == typeof(global::Mediator.IMediator).Assembly)));{newLine}";
        List<StatementSyntax> replacement = [.. SyntaxFactory.ParseSyntaxTree(
            $"class C{{void M(){{{newLine}{code}}}}}", (CSharpParseOptions)block.SyntaxTree.Options).GetRoot()
            .DescendantNodes().OfType<BlockSyntax>().First().Statements];
        // Blank lines and comments in front of the scan stay in front of the replacement; its own indentation comes with the new code.
        SyntaxTriviaList before = scan.GetLeadingTrivia();
        SyntaxTriviaList leading = SyntaxFactory.TriviaList(
            before.Take(before.Count - before.Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).Count()).Concat(replacement[0].GetLeadingTrivia()));
        replacement[0] = replacement[0].WithLeadingTrivia(leading);

        BlockSyntax rewritten = block.ReplaceNodes(
            [.. removedTypes.Select(t => t.Parent!).OfType<SyntaxNode>().Distinct().Where(p => p is InitializerExpressionSyntax or CollectionExpressionSyntax)],
            (original, _) => RemoveElements(original, removedTypes));
        rewritten = rewritten.WithStatements([.. rewritten.Statements.SelectMany((s, i) =>
            block.Statements[i] == scan ? replacement
            : dropped.Contains(block.Statements[i]) ? []
            : (IEnumerable<StatementSyntax>)[s])]);
        return PatternResult.Fixed(rewritten, "System.Linq", "Microsoft.Extensions.DependencyInjection", "Autofac.Extensions.DependencyInjection");
    }

    // builder.RegisterAssemblyTypes(typeof(T).Assembly) or (typeof(T).GetTypeInfo().Assembly), T of MediatR, at the root of the statement's call chain.
    private static InvocationExpressionSyntax? ScanOfMediatR(ExpressionStatementSyntax statement, SemanticModel model)
    {
        ExpressionSyntax current = statement.Expression;
        while (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax member } invocation)
        {
            if (member.Name.Identifier.ValueText == "RegisterAssemblyTypes")
            {
                return invocation.ArgumentList.Arguments is [{ Expression: var argument }]
                    && TypeOfAssembly(argument) is TypeOfExpressionSyntax typeOf
                    && model.GetTypeInfo(typeOf.Type).Type?.ContainingAssembly?.Name == "MediatR"
                    && model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ContainingNamespace: var ns }
                    && ns.ToDisplayString().StartsWith("Autofac", StringComparison.Ordinal)
                        ? invocation
                        : null;
            }

            current = member.Expression;
        }

        return null;
    }

    private static TypeOfExpressionSyntax? TypeOfAssembly(ExpressionSyntax expression) => expression switch
    {
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Assembly", Expression: TypeOfExpressionSyntax typeOf } => typeOf,
        MemberAccessExpressionSyntax
        {
            Name.Identifier.ValueText: "Assembly",
            Expression: InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0, Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "GetTypeInfo", Expression: TypeOfExpressionSyntax typeOf } },
        } => typeOf,
        _ => null,
    };

    // The calls after RegisterAssemblyTypes: AsImplementedInterfaces (required: it registered IMediator) and at most one lifetime.
    private static string? Lifetime(ExpressionStatementSyntax statement, InvocationExpressionSyntax register)
    {
        List<string> calls = [];
        for (ExpressionSyntax current = statement.Expression; current != register;)
        {
            if (current is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: 0, Expression: MemberAccessExpressionSyntax member })
            {
                return null;
            }

            calls.Add(member.Name.Identifier.ValueText);
            current = member.Expression;
        }

        if (!calls.Remove("AsImplementedInterfaces"))
        {
            return null;
        }

        // Autofac's default is a new instance per dependency.
        return calls switch
        {
            [] or ["InstancePerDependency"] => "ServiceLifetime.Transient",
            ["InstancePerLifetimeScope"] => "ServiceLifetime.Scoped",
            ["SingleInstance"] => "ServiceLifetime.Singleton",
            _ => null,
        };
    }

    // x.RegisterGeneric(typeof(MediatR.Pipeline.X<,>)).As(typeof(IPipelineBehavior<,>)) → X.
    private static INamedTypeSymbol? RegisteredPipelineType(ExpressionStatementSyntax statement, SemanticModel model) =>
        statement.Expression is InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "As",
                Expression: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "RegisterGeneric" },
                    ArgumentList.Arguments: [{ Expression: TypeOfExpressionSyntax behavior }],
                },
            },
            ArgumentList.Arguments: [{ Expression: TypeOfExpressionSyntax service }],
        }
        && model.GetTypeInfo(service.Type).Type is INamedTypeSymbol { MetadataName: "IPipelineBehavior`2", ContainingAssembly.Name: "MediatR" }
        && model.GetTypeInfo(behavior.Type).Type is INamedTypeSymbol type
        && IsPipelineType(type)
            ? type
            : null;

    private static bool IsPipelineType(ITypeSymbol? type) =>
        type is INamedTypeSymbol { ContainingAssembly.Name: "MediatR" } named && named.ContainingNamespace.ToDisplayString() == PipelineNamespace;

    private static bool IsListElement(TypeOfExpressionSyntax typeOf) =>
        typeOf.Parent is InitializerExpressionSyntax { RawKind: (int)SyntaxKind.ArrayInitializerExpression or (int)SyntaxKind.CollectionInitializerExpression }
        || typeOf.Parent is ExpressionElementSyntax { Parent: CollectionExpressionSyntax };

    // Classes implementing a MediatR processor interface in this project or a project it references.
    private static IEnumerable<INamedTypeSymbol> Processors(Compilation compilation, string metadataName) =>
        new[] { compilation.Assembly }.Concat(compilation.SourceModule.ReferencedAssemblySymbols
                .Where(a => a.Modules.Any(m => m.ReferencedAssemblies.Any(r => r.Name == "MediatR"))))
            .SelectMany(a => AddMediatRPattern.Types(a.GlobalNamespace))
            .Where(t => t is { TypeKind: TypeKind.Class, IsAbstract: false }
                && t.AllInterfaces.Any(i => i.MetadataName == metadataName && i.ContainingNamespace.ToDisplayString() == PipelineNamespace));

    private static SyntaxNode RemoveElements(SyntaxNode list, List<ExpressionSyntax> removed) => list switch
    {
        InitializerExpressionSyntax initializer => initializer.WithExpressions(
            SyntaxFactory.SeparatedList(initializer.Expressions.Where(e => !removed.Any(r => r.Span == e.Span)), Separators(initializer.Expressions, removed))),
        CollectionExpressionSyntax collection => collection.WithElements(
            SyntaxFactory.SeparatedList(collection.Elements.Where(e => e is not ExpressionElementSyntax element || !removed.Any(r => r.Span == element.Expression.Span)))),
        _ => list,
    };

    // Keeps the separators (and their line breaks) of the elements that stay; a trailing comma stays when it was there.
    private static IEnumerable<SyntaxToken> Separators(SeparatedSyntaxList<ExpressionSyntax> expressions, List<ExpressionSyntax> removed)
    {
        List<SyntaxToken> kept = [];
        for (int i = 0; i < expressions.Count; i++)
        {
            if (!removed.Any(r => r.Span == expressions[i].Span) && i < expressions.SeparatorCount)
            {
                kept.Add(expressions.GetSeparator(i));
            }
        }

        int count = expressions.Count(e => !removed.Any(r => r.Span == e.Span));
        return count == 0 ? [] : kept.Take(expressions.SeparatorCount == expressions.Count ? count : count - 1);
    }

    private static string FreeName(SemanticModel model, int position, string name)
    {
        string candidate = name;
        for (int i = 2; model.LookupSymbols(position, name: candidate).Length > 0; i++)
        {
            candidate = name + i;
        }

        return candidate;
    }
}
