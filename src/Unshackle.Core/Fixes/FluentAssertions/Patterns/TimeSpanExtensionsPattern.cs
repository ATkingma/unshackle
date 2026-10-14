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

    public string Description => "5.Seconds() / 200.Milliseconds() → TimeSpan.FromSeconds(5) / TimeSpan.FromMilliseconds(200); 1.March(2020) → new DateTime(2020, 3, 1); " +
        "1.March(2020).At(12, 30) → new DateTime(2020, 3, 1, 12, 30)";

    // A month call that is the date of At(...) is rewritten together with that At (TAAK-243).
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(i => Method(i, model) is not null
                && !(i.Parent is MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax outer } && Method(outer, model) is { Name: "At" }));

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

        if (method.Name == "At" && definition.Parameters[0].Type.SpecialType == SpecialType.System_DateTime)
        {
            return RewriteAt(invocation, arguments, definition, model);
        }

        int month = Array.IndexOf(s_months, method.Name) + 1;
        if (month > 0 && arguments.Count == 2)
        {
            if (MonthReason(method.Name, month, arguments, model) is string reason)
            {
                return PatternResult.Manual(reason);
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

    // FluentAssertions builds `new DateTime(date.Year, date.Month, date.Day, hours, minutes, seconds, milliseconds, date.Kind)`,
    // or `date.Date + time` for a TimeSpan (TAAK-243). A month call as the date becomes one constructor (Kind Unspecified, like FluentAssertions).
    private static PatternResult RewriteAt(InvocationExpressionSyntax invocation, List<ExpressionSyntax> arguments, IMethodSymbol definition, SemanticModel model)
    {
        ExpressionSyntax date = arguments[0];
        (string dateTime, string[] namespaces) = TypeName("DateTime", model, invocation.SpanStart);
        string? monthDate = null;
        if (date is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax dayAccess, ArgumentList.Arguments: [{ NameColon: null } year] } monthCall
            && Method(monthCall, model) is { ReducedFrom: not null } monthMethod
            && Array.IndexOf(s_months, monthMethod.Name) + 1 is > 0 and int month)
        {
            ExpressionSyntax[] parts = [dayAccess.Expression, year.Expression];
            if (MonthReason(monthMethod.Name, month, parts, model) is string reason)
            {
                return PatternResult.Manual(reason);
            }

            monthDate = $"{Unwrap(parts[1])}, {month}, {Unwrap(parts[0])}";
        }

        if (definition.Parameters is [_, { Type.Name: "TimeSpan" }] && arguments.Count == 2)
        {
            string sum = monthDate is null ? $"{Unwrap(date)}.Date + {Unwrap(arguments[1])}" : $"new {dateTime}({monthDate}) + {Unwrap(arguments[1])}";
            ExpressionSyntax expression = SyntaxFactory.ParseExpression(sum);
            return PatternResult.Fixed(NeedsParentheses(invocation) ? SyntaxFactory.ParenthesizedExpression(expression) : expression, namespaces);
        }

        if (arguments.Count is < 3 or > 5 || definition.Parameters.Skip(1).Any(p => p.Type.SpecialType != SpecialType.System_Int32))
        {
            return PatternResult.Manual("At(...) with microseconds or nanoseconds has no DateTime constructor: rewrite it by hand (e.g. new DateTime(..).AddMicroseconds(..)).");
        }

        string[] time = [.. arguments.Skip(1).Select(a => Unwrap(a).ToString())];
        if (monthDate is not null)
        {
            // DateTime has no constructor with hours and minutes only.
            string[] withSeconds = [.. time, .. Enumerable.Repeat("0", Math.Max(0, 3 - time.Length))];
            return PatternResult.Fixed(SyntaxFactory.ParseExpression($"new {dateTime}({monthDate}, {string.Join(", ", withSeconds)})"), namespaces);
        }

        if (!FluentAssertionsSyntax.IsSafeToEvaluateTwice(date, model))
        {
            return PatternResult.Manual(
                "At(...) on a computed date: put the date in a local first, then write new DateTime(d.Year, d.Month, d.Day, hours, minutes, seconds, milliseconds, d.Kind).");
        }

        string d = Unwrap(date).ToString();
        string[] clock = [.. time, .. Enumerable.Repeat("0", 4 - time.Length)];
        return PatternResult.Fixed(
            SyntaxFactory.ParseExpression($"new {dateTime}({d}.Year, {d}.Month, {d}.Day, {string.Join(", ", clock)}, {d}.Kind)"),
            namespaces);
    }

    // FluentAssertions evaluates the day first, the constructor takes the year first: only safe when one is constant.
    private static string? MonthReason(string name, int month, IReadOnlyList<ExpressionSyntax> dayAndYear, SemanticModel model) =>
        model.GetConstantValue(dayAndYear[0]).HasValue || model.GetConstantValue(dayAndYear[1]).HasValue
            ? null
            : $"{name}(...) with a computed day and year: write new DateTime(year, {month}, day) by hand (mind the evaluation order).";

    // `d.Date + t` replaces a call: it needs parentheses where an operand or receiver stood (`.At(t).ToString()`, `x - d.At(t)`).
    private static bool NeedsParentheses(InvocationExpressionSyntax invocation) =>
        invocation.Parent is not (ArgumentSyntax or EqualsValueClauseSyntax or ArrowExpressionClauseSyntax or ReturnStatementSyntax
            or ExpressionElementSyntax or InitializerExpressionSyntax or ExpressionStatementSyntax or ParenthesizedExpressionSyntax)
            && !(invocation.Parent is AssignmentExpressionSyntax assignment && assignment.Right == invocation);

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
