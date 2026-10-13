using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.FluentAssertions.Patterns;

// FluentAssertions.Extensions builds times fluently (`5.Seconds()`, `1.March(2020)`). They are no assertions, often a
// timeout argument outside any assert (Akka TestKit: `ExpectMsg(m, 3.Seconds())`), and keep the package in the project.
// FluentAssertions implements them with the same TimeSpan factory and DateTime constructor we write (TAAK-224).
public class TimeSpanExtensionsPattern : IPattern
{
    private const string ExtensionsNamespace = "FluentAssertions.Extensions";

    private static readonly Dictionary<string, string> s_factories = new(StringComparer.Ordinal)
    {
        ["Ticks"] = "FromTicks",
        ["Milliseconds"] = "FromMilliseconds",
        ["Seconds"] = "FromSeconds",
        ["Minutes"] = "FromMinutes",
        ["Hours"] = "FromHours",
        ["Days"] = "FromDays",
    };

    private static readonly string[] s_months =
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    // Literal for check-pattern-docs.sh.
    public string Id => "fa.timespan-extensions";

    public string Description => "5.Seconds() / 200.Milliseconds() → TimeSpan.FromSeconds(5) / TimeSpan.FromMilliseconds(200); 1.March(2020) → new DateTime(2020, 3, 1)";

    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => Method(i, model) is not null);

    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        var invocation = (InvocationExpressionSyntax)node;
        IMethodSymbol method = Method(invocation, model)!;
        // Reduced (`5.Seconds()`) or static (`FluentTimeSpanExtensions.Seconds(5)`): the same arguments in order.
        List<ExpressionSyntax> arguments = [.. invocation.ArgumentList.Arguments.Select(a => a.Expression)];
        if (method.ReducedFrom is not null && invocation.Expression is MemberAccessExpressionSyntax access)
        {
            arguments.Insert(0, access.Expression);
        }
        else if (method.ReducedFrom is not null || invocation.ArgumentList.Arguments.Any(a => a.NameColon is not null))
        {
            return PatternResult.Manual($"{method.Name}(...) from {ExtensionsNamespace} in an unusual form: rewrite it by hand.");
        }

        IMethodSymbol definition = method.ReducedFrom ?? method;
        bool fromNumber = definition.Parameters is [{ Type.SpecialType: SpecialType.System_Int32 or SpecialType.System_Int64 or SpecialType.System_Double }];
        if (s_factories.TryGetValue(method.Name, out string? factory) && fromNumber && arguments.Count == 1)
        {
            (string timeSpan, string[] namespaces) = TypeName("TimeSpan", model, invocation.SpanStart);
            return PatternResult.Fixed(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.ParseExpression($"{timeSpan}.{factory}"),
                    SyntaxFactory.ArgumentList([SyntaxFactory.Argument(Unwrap(arguments[0]))])),
                namespaces);
        }

        int month = Array.IndexOf(s_months, method.Name) + 1;
        if (month > 0 && arguments.Count == 2)
        {
            // FluentAssertions evaluates the day first, the constructor takes the year first: only safe when one is constant.
            if (!model.GetConstantValue(arguments[0]).HasValue && !model.GetConstantValue(arguments[1]).HasValue)
            {
                return PatternResult.Manual(
                    $"{method.Name}(...) with a computed day and year: write new DateTime(year, {month}, day) by hand (mind the evaluation order).");
            }

            (string dateTime, string[] namespaces) = TypeName("DateTime", model, invocation.SpanStart);
            return PatternResult.Fixed(
                SyntaxFactory.ParseExpression($"new {dateTime}({Unwrap(arguments[1])}, {month}, {Unwrap(arguments[0])})"),
                namespaces);
        }

        return PatternResult.Manual(
            $"{method.ContainingType.Name}.{method.Name}(...) from {ExtensionsNamespace} has no direct replacement: rewrite it by hand " +
            "(e.g. TimeSpan.FromSeconds(a) + b, dateTime.Add(..), DateTime.SpecifyKind(..)).");
    }

    private static IMethodSymbol? Method(InvocationExpressionSyntax invocation, SemanticModel model) =>
        model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { ContainingType.Name: "FluentTimeSpanExtensions" or "FluentDateTimeExtensions" } method
            && method.ContainingNamespace.ToDisplayString() == ExtensionsNamespace
            ? method
            : null;

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression) =>
        (expression is ParenthesizedExpressionSyntax parenthesized ? parenthesized.Expression : expression).WithoutTrivia();

    // The simple name when it binds to the System type here (or nothing yet: then `using System;` is added); otherwise qualified.
    private static (string Name, string[] Namespaces) TypeName(string name, SemanticModel model, int position)
    {
        ISymbol[] bound = [.. model.LookupNamespacesAndTypes(position, name: name)];
        return bound switch
        {
            [] => (name, ["System"]),
            [INamedTypeSymbol type] when type.ToDisplayString() == "System." + name => (name, []),
            _ => ("global::System." + name, []),
        };
    }
}
