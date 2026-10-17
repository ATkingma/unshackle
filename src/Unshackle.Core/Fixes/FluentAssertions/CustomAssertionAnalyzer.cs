using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Unshackle.Core.Fixes.FluentAssertions;

// Decides whether a user-written `class XAssertions : ReferenceTypeAssertions<X, XAssertions>` has a provably equivalent
// Shouldly form, and writes it: a static class of `ShouldFoo(this X subject, ..., string? customMessage = null)` extension
// methods that throw ShouldAssertException exactly when `Execute.Assertion.ForCondition(...).FailWith(...)` would have
// failed (probed against FluentAssertions 7.2.2, TAAK-275). Only that closed vocabulary converts: anything else (state,
// helpers, Given/Then, lazy messages, other statements) is a precise reason for Manual.
public static class CustomAssertionAnalyzer
{
    private const string ShouldlyException = "ShouldAssertException";

    public static CustomAssertionShape Analyze(ClassDeclarationSyntax declaration, SemanticModel model)
    {
        if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol)
        {
            return CustomAssertionShape.Fail("the class declaration has no symbol");
        }

        if (declaration.TypeParameterList is not null || declaration.Modifiers.Any(SyntaxKind.PartialKeyword) || symbol.ContainingType is { IsGenericType: true })
        {
            return CustomAssertionShape.Fail("the class is generic or partial");
        }

        if (symbol.BaseType is not { Name: "ReferenceTypeAssertions", TypeArguments: [{ } subjectType, { } self] } baseType
            || !FluentAssertionsSyntax.IsFluentAssertions(baseType)
            || !SymbolEqualityComparer.Default.Equals(self, symbol))
        {
            return CustomAssertionShape.Fail(
                $"it derives from {symbol.BaseType?.ToDisplayString()}; only ReferenceTypeAssertions<T, itself> has a Shouldly form here");
        }

        string subjectText = subjectType.ToMinimalDisplayString(model, declaration.SpanStart);
        bool nullable = (model.GetNullableContext(declaration.SpanStart) & NullableContext.AnnotationsEnabled) != 0;
        string? identifier = null;
        bool constructed = false;
        var methods = new List<(string Name, IReadOnlyList<string> Lines)>();
        if (declaration.ParameterList is not null)
        {
            string? primary = CheckPrimaryConstructor(declaration, model);
            if (primary is not null)
            {
                return CustomAssertionShape.Fail(primary);
            }

            constructed = true;
        }

        foreach (MemberDeclarationSyntax member in declaration.Members)
        {
            switch (member)
            {
                case ConstructorDeclarationSyntax constructor:
                    if (constructed || !PassesSubjectToBase(constructor))
                    {
                        return CustomAssertionShape.Fail("the constructor does more than pass the subject to the base class");
                    }

                    constructed = true;
                    break;
                case PropertyDeclarationSyntax { Identifier.ValueText: "Identifier", ExpressionBody: { } body }
                    when model.GetConstantValue(body.Expression).Value is string text:
                    identifier = text;
                    break;
                case MethodDeclarationSyntax method:
                    (string? reason, IReadOnlyList<string> lines) = ConvertMethod(method, model, symbol, subjectType, subjectText, identifier, nullable);
                    if (reason is not null)
                    {
                        return CustomAssertionShape.Fail($"{method.Identifier.ValueText}: {reason}");
                    }

                    methods.Add((method.Identifier.ValueText, lines));
                    break;
                default:
                    return CustomAssertionShape.Fail($"it has a member that is not an assertion method ({Describe(member)})");
            }
        }

        if (!constructed || methods.Count == 0)
        {
            return CustomAssertionShape.Fail("it has no constructor that takes the subject, or no assertion method");
        }

        string modifiers = string.Concat(declaration.Modifiers
            .Where(m => m.Kind() is SyntaxKind.PublicKeyword or SyntaxKind.InternalKeyword or SyntaxKind.ProtectedKeyword or SyntaxKind.PrivateKeyword)
            .Select(m => m.Text + " "));
        string name = declaration.Identifier.ValueText;
        return new CustomAssertionShape
        {
            MethodNames = [.. methods.Select(m => m.Name)],
            SubjectType = subjectText,
            Render = (indentation, eol) =>
            {
                var builder = new StringBuilder();
                builder.Append(modifiers).Append("static class ").Append(name).Append(eol).Append(indentation).Append('{').Append(eol);
                for (int i = 0; i < methods.Count; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(eol);
                    }

                    foreach (string line in methods[i].Lines)
                    {
                        builder.Append(line.Length == 0 ? string.Empty : indentation + "    " + line).Append(eol);
                    }
                }

                return builder.Append(indentation).Append('}').ToString();
            },
        };
    }

    private static string Describe(MemberDeclarationSyntax member) =>
        member switch
        {
            MethodDeclarationSyntax method => $"method {method.Identifier.ValueText}",
            PropertyDeclarationSyntax property => $"property {property.Identifier.ValueText}",
            FieldDeclarationSyntax field => $"field {field.Declaration.Variables[0].Identifier.ValueText}",
            _ => member.Kind().ToString(),
        };

    // `class C(T subject) : ReferenceTypeAssertions<T, C>(subject)`: the parameter may only reach the base class.
    private static string? CheckPrimaryConstructor(ClassDeclarationSyntax declaration, SemanticModel model)
    {
        if (declaration.ParameterList!.Parameters is not [{ } parameter]
            || declaration.BaseList?.Types.FirstOrDefault() is not PrimaryConstructorBaseTypeSyntax { ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax argument }] }
            || argument.Identifier.ValueText != parameter.Identifier.ValueText)
        {
            return "the primary constructor does more than pass the subject to the base class";
        }

        return declaration.Members
            .SelectMany(m => m.DescendantNodes().OfType<IdentifierNameSyntax>())
            .Any(n => model.GetSymbolInfo(n).Symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Constructor } })
                ? "the primary constructor parameter is used in the class"
                : null;
    }

    private static bool PassesSubjectToBase(ConstructorDeclarationSyntax constructor) =>
        constructor.ParameterList.Parameters is [{ } parameter]
        && constructor.Initializer is { ArgumentList.Arguments: [{ Expression: IdentifierNameSyntax argument }] } initializer
        && initializer.IsKind(SyntaxKind.BaseConstructorInitializer)
        && argument.Identifier.ValueText == parameter.Identifier.ValueText
        && constructor.Body is { Statements.Count: 0 };

    // The extension method for one assertion method: its parameters, and per Execute.Assertion statement a guard that throws.
    private static (string? Reason, IReadOnlyList<string> Lines) ConvertMethod(
        MethodDeclarationSyntax method,
        SemanticModel model,
        INamedTypeSymbol assertions,
        ITypeSymbol subjectType,
        string subjectText,
        string? identifier,
        bool nullable)
    {
        if (!method.Modifiers.Any(SyntaxKind.PublicKeyword) || method.Modifiers.Any(m => m.Kind() is SyntaxKind.StaticKeyword or SyntaxKind.AsyncKeyword)
            || method.TypeParameterList is not null || method.Body is null || model.GetDeclaredSymbol(method) is not IMethodSymbol symbol)
        {
            return ("it is not a public, non-generic, non-async instance method with a block body", []);
        }

        IReadOnlyList<ParameterSyntax> parameters = method.ParameterList.Parameters;
        bool hasBecause = parameters is [.., { Identifier.ValueText: "because" }, { Identifier.ValueText: "becauseArgs" }];
        if (!hasBecause && parameters.Any(p => p.Identifier.ValueText is "because" or "becauseArgs"))
        {
            return ("because/becauseArgs are not the last two parameters", []);
        }

        IParameterSymbol? because = hasBecause ? symbol.Parameters[^2] : null;
        IParameterSymbol? becauseArgs = hasBecause ? symbol.Parameters[^1] : null;
        if (hasBecause && (because!.Type.SpecialType != SpecialType.System_String
            || becauseArgs!.Type is not IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Object } || !becauseArgs.IsParams))
        {
            return ("because/becauseArgs are not (string because, params object[] becauseArgs)", []);
        }

        IReadOnlyList<ParameterSyntax> others = [.. parameters.Take(parameters.Count - (hasBecause ? 2 : 0))];
        if (others.Any(p => p.Modifiers.Count > 0 || p.AttributeLists.Count > 0))
        {
            return ("a parameter has a modifier (ref/out/in/params) or an attribute", []);
        }

        bool isVoid = method.ReturnType is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword };
        List<StatementSyntax> statements = [.. method.Body.Statements];
        if (!isVoid)
        {
            if (model.GetTypeInfo(method.ReturnType).Type is not INamedTypeSymbol { Name: "AndConstraint", TypeArguments: [{ } constrained] } returned
                || !FluentAssertionsSyntax.IsFluentAssertions(returned)
                || (!SymbolEqualityComparer.Default.Equals(constrained, assertions) && !SymbolEqualityComparer.Default.Equals(constrained, subjectType)))
            {
                return ("it returns something else than AndConstraint<itself> or AndConstraint<subject> (e.g. AndWhichConstraint)", []);
            }

            if (statements is not [.., ReturnStatementSyntax { Expression: ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax } last]
                || !ReturnsConstraintOfThis(last.Expression!))
            {
                return ("it does not end with `return new AndConstraint<...>(this)`", []);
            }

            statements.RemoveAt(statements.Count - 1);
        }

        HashSet<string> used = [.. method.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken)).Select(t => t.ValueText)];
        string subject = Unique(used, "subject", "actual", "instance");
        string customMessage = Unique(used, "customMessage", "reasonText");
        var guards = new List<string>();
        var assertionsFound = new List<(ExpressionSyntax Condition, string Format, IReadOnlyList<ExpressionSyntax> Args)>();
        foreach (StatementSyntax statement in statements)
        {
            if (statement is not ExpressionStatementSyntax expression)
            {
                return ($"the statement `{Shorten(statement.ToString())}` is not an Execute.Assertion chain", []);
            }

            string? reason = ParseAssertion(expression, model, because, becauseArgs, out (ExpressionSyntax, string, IReadOnlyList<ExpressionSyntax>) assertion);
            if (reason is not null)
            {
                return (reason, []);
            }

            assertionsFound.Add(assertion);
        }

        if (assertionsFound.Count == 0)
        {
            return ("it has no Execute.Assertion chain", []);
        }

        for (int i = 0; i < assertionsFound.Count; i++)
        {
            string suffix = assertionsFound.Count == 1 ? string.Empty : (i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            (string? reason, IReadOnlyList<string> guard) = Guard(
                assertionsFound[i], model, assertions, subject, customMessage, hasBecause, identifier ?? "object", suffix, used);
            if (reason is not null)
            {
                return (reason, []);
            }

            guards.AddRange(guard);
        }

        string stringType = nullable ? "string?" : "string";
        List<string> parameterTexts =
        [
            $"this {subjectText} {subject}",
            .. others.Select(p => p.WithoutTrivia().ToString()),
            .. hasBecause ? [$"{stringType} {customMessage} = null"] : Array.Empty<string>(),
        ];
        var lines = new List<string>
        {
            $"public static void Should{method.Identifier.ValueText}({string.Join(", ", parameterTexts)})",
            "{",
        };
        lines.AddRange(guards.Select(g => g.Length == 0 ? g : "    " + g));
        lines.Add("}");
        return (null, lines);
    }

    private static bool ReturnsConstraintOfThis(ExpressionSyntax creation)
    {
        ArgumentListSyntax? arguments = (creation as BaseObjectCreationExpressionSyntax)?.ArgumentList;
        return arguments is { Arguments: [{ NameColon: null, Expression: ThisExpressionSyntax or IdentifierNameSyntax { Identifier.ValueText: "Subject" } }] };
    }

    // `Execute.Assertion[.BecauseOf(because, becauseArgs)].ForCondition(c).FailWith("format", args)`, the calls in any order
    // except FailWith last. Probed: ForCondition(false) makes FailWith throw, true makes it a no-op, the arguments are evaluated
    // either way, `because` shows only through {reason}, and outside an AssertionScope the first failure throws.
    private static string? ParseAssertion(
        ExpressionStatementSyntax statement,
        SemanticModel model,
        IParameterSymbol? because,
        IParameterSymbol? becauseArgs,
        out (ExpressionSyntax Condition, string Format, IReadOnlyList<ExpressionSyntax> Args) assertion)
    {
        assertion = default;
        ExpressionSyntax current = statement.Expression;
        var calls = new List<(string Name, InvocationExpressionSyntax Invocation)>();
        while (current is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax access } invocation)
        {
            calls.Insert(0, (access.Name.Identifier.ValueText, invocation));
            current = access.Expression;
        }

        if (current is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Assertion" } root
            || model.GetSymbolInfo(root).Symbol is not IPropertySymbol { ContainingType.Name: "Execute" } property
            || !FluentAssertionsSyntax.IsFluentAssertions(property))
        {
            return $"the statement `{Shorten(statement.ToString())}` is not an Execute.Assertion chain";
        }

        string[] names = [.. calls.Select(c => c.Name)];
        if (names.Length == 0 || names[^1] != "FailWith" || names.Count(n => n == "ForCondition") != 1 || names.Count(n => n == "FailWith") != 1
            || names.Count(n => n == "BecauseOf") > 1 || names.Any(n => n is not ("FailWith" or "ForCondition" or "BecauseOf")))
        {
            return $"`Execute.Assertion.{string.Join(".", names)}` is not ForCondition(...) and FailWith(...) with an optional BecauseOf(because, becauseArgs) "
                + "(Given/Then, Then chains and other calls have no provably equal Shouldly form)";
        }

        if (calls.Any(c => model.GetSymbolInfo(c.Invocation).Symbol is not IMethodSymbol method || !FluentAssertionsSyntax.IsFluentAssertions(method)))
        {
            return "an Execute.Assertion call is not a FluentAssertions method";
        }

        ExpressionSyntax? condition = null;
        string? format = null;
        IReadOnlyList<ExpressionSyntax> args = [];
        foreach ((string name, InvocationExpressionSyntax invocation) in calls)
        {
            var method = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;
            IReadOnlyList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
            if (arguments.Any(a => a.NameColon is not null))
            {
                return $"{name}(...) with named arguments";
            }

            switch (name)
            {
                case "BecauseOf":
                    if (because is null || arguments is not [{ Expression: IdentifierNameSyntax first }, { Expression: IdentifierNameSyntax second }]
                        || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(first).Symbol, because)
                        || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(second).Symbol, becauseArgs))
                    {
                        return "BecauseOf is not BecauseOf(because, becauseArgs) of the method's own parameters";
                    }

                    break;
                case "ForCondition":
                    if (method.Parameters is not [{ Type.SpecialType: SpecialType.System_Boolean }] || arguments.Count != 1)
                    {
                        return "ForCondition does not take a bool (a Given/Then predicate is evaluated lazily)";
                    }

                    condition = arguments[0].Expression;
                    break;
                default:
                    if (method.Parameters is not [{ Type.SpecialType: SpecialType.System_String }, { IsParams: true, Type: IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_Object } }]
                        || arguments.Count == 0 || model.GetConstantValue(arguments[0].Expression).Value is not string text
                        || model.GetOperation(invocation) is not IInvocationOperation { Arguments: [_, { ArgumentKind: ArgumentKind.ParamArray }] })
                    {
                        return "FailWith is not FailWith(constant format, arguments...) (a lazy message or a failure-reason object has no provably equal form)";
                    }

                    format = text;
                    args = [.. arguments.Skip(1).Select(a => a.Expression)];
                    break;
            }
        }

        assertion = (condition!, format!, args);
        return null;
    }

    // The guard for one assertion: the condition and the arguments are evaluated like FluentAssertions does (condition first,
    // then every argument, whether or not it fails); then a failing condition throws ShouldAssertException.
    private static (string? Reason, IReadOnlyList<string> Lines) Guard(
        (ExpressionSyntax Condition, string Format, IReadOnlyList<ExpressionSyntax> Args) assertion,
        SemanticModel model,
        INamedTypeSymbol assertions,
        string subject,
        string customMessage,
        bool hasBecause,
        string defaultContext,
        string suffix,
        HashSet<string> used)
    {
        string? reason = Rewrite(assertion.Condition, model, assertions, subject, out string condition);
        var texts = new List<string>();
        var hoisted = new List<string>();
        var argumentTexts = new List<string>();
        for (int i = 0; reason is null && i < assertion.Args.Count; i++)
        {
            ExpressionSyntax argument = assertion.Args[i];
            reason = Rewrite(argument, model, assertions, subject, out string text);
            bool trivial = argument is LiteralExpressionSyntax || model.GetConstantValue(argument).HasValue
                || model.GetSymbolInfo(argument).Symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol { MethodKind: MethodKind.Ordinary } };
            if (trivial)
            {
                argumentTexts.Add(text);
                continue;
            }

            string local = Unique(used, $"actual{suffix}{(assertion.Args.Count > 1 ? i.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty)}");
            used.Add(local);
            hoisted.Add($"var {local} = {text};");
            argumentTexts.Add(local);
        }

        if (reason is not null)
        {
            return (reason, []);
        }

        string reasonText = $"({customMessage} is null ? \"\" : \" because \" + {customMessage})";
        if (!FormatMessage(assertion.Format, argumentTexts, hasBecause ? reasonText : "\"\"", defaultContext, out string message, out string? formatReason))
        {
            return (formatReason, []);
        }

        var lines = new List<string>();
        string test;
        if (hoisted.Count > 0)
        {
            string flag = Unique(used, $"condition{suffix}");
            used.Add(flag);
            lines.Add($"bool {flag} = {condition};");
            lines.AddRange(hoisted);
            test = $"!{flag}";
        }
        else
        {
            test = Negate(assertion.Condition, condition);
        }

        lines.Add($"if ({test})");
        lines.Add("{");
        lines.Add($"    throw new {ShouldlyException}({message});");
        lines.Add("}");
        texts.AddRange(lines);
        return (null, texts);
    }

    private static string Negate(ExpressionSyntax original, string text) =>
        original is PrefixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.LogicalNotExpression, Operand: var operand } && !text.StartsWith("!(", StringComparison.Ordinal)
            ? text[1..]
            : FluentAssertionsSyntax.IsSafeReceiver(original) || original is LiteralExpressionSyntax
                ? "!" + text
                : $"!({text})";

    // The expression as text with `Subject` (and this.Subject) replaced by the extension's parameter. Anything else that
    // reaches into the assertions class or FluentAssertions has no counterpart in the extension method.
    private static string? Rewrite(ExpressionSyntax expression, SemanticModel model, INamedTypeSymbol assertions, string subject, out string text)
    {
        text = string.Empty;
        var replace = new List<SyntaxNode>();
        foreach (SyntaxNode node in expression.DescendantNodesAndSelf())
        {
            if (node is ThisExpressionSyntax or BaseExpressionSyntax
                && !(node.Parent is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } access && access.Expression == node && IsSubject(model.GetSymbolInfo(name).Symbol)))
            {
                return "it uses `this` (state of the assertions class)";
            }

            if (node is not IdentifierNameSyntax identifier)
            {
                continue;
            }

            ISymbol? symbol = model.GetSymbolInfo(identifier).Symbol;
            if (IsSubject(symbol))
            {
                replace.Add(identifier.Parent is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax or BaseExpressionSyntax } qualified && qualified.Name == identifier
                    ? qualified
                    : identifier);
            }
            else if (symbol is not null
                && (SymbolEqualityComparer.Default.Equals(symbol.ContainingType, assertions)
                    || FluentAssertionsSyntax.IsFluentAssertions(symbol)
                    || FluentAssertionsSyntax.IsFluentAssertions(symbol.ContainingType)))
            {
                return $"it uses `{symbol.Name}` of the assertions class or FluentAssertions";
            }
        }

        SyntaxNode rewritten = replace.Contains(expression)
            ? SyntaxFactory.IdentifierName(subject)
            : expression.ReplaceNodes(replace, (_, _) => SyntaxFactory.IdentifierName(subject));
        text = rewritten.WithoutTrivia().NormalizeWhitespace().ToString();
        return null;
    }

    private static bool IsSubject(ISymbol? symbol) =>
        symbol is IPropertySymbol { Name: "Subject" } property && FluentAssertionsSyntax.IsFluentAssertions(property.ContainingType);

    // The interpolated string for a FailWith format: {context:name} is the name, {reason} the because text, {n} argument n.
    // Placeholders FluentAssertions fills in another way (escaped braces, format specifiers, names) have no counterpart here.
    private static bool FormatMessage(
        string format, IReadOnlyList<string> arguments, string reason, string defaultContext, out string message, out string? failure)
    {
        message = string.Empty;
        failure = null;
        var builder = new StringBuilder();
        bool interpolated = false;
        var text = new StringBuilder();
        void FlushText()
        {
            builder.Append(SymbolDisplay.FormatLiteral(text.ToString(), quote: false));
            text.Clear();
        }

        for (int i = 0; i < format.Length; i++)
        {
            char c = format[i];
            if (c == '}' || (c == '{' && (format.IndexOf('}', i + 1) is var end && (end < 0 || format.IndexOf('{', i + 1, end - i - 1) >= 0))))
            {
                failure = "the FailWith format has escaped or unbalanced braces";
                return false;
            }

            if (c != '{')
            {
                text.Append(c);
                continue;
            }

            string placeholder = format[(i + 1)..format.IndexOf('}', i + 1)];
            i += placeholder.Length + 1;
            FlushText();
            interpolated = true;
            if (placeholder == "reason")
            {
                builder.Append('{').Append(reason).Append('}');
            }
            else if (placeholder == "context" || placeholder.StartsWith("context:", StringComparison.Ordinal))
            {
                string context = placeholder == "context" ? defaultContext : placeholder["context:".Length..];
                builder.Append(SymbolDisplay.FormatLiteral(context, quote: false).Replace("{", "{{", StringComparison.Ordinal).Replace("}", "}}", StringComparison.Ordinal));
            }
            else if (int.TryParse(placeholder, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int index) && index < arguments.Count)
            {
                builder.Append('{').Append(arguments[index]).Append('}');
            }
            else
            {
                failure = $"the FailWith placeholder {{{placeholder}}} is not {{context}}, {{reason}} or {{n}}";
                return false;
            }
        }

        FlushText();
        message = interpolated ? "$\"" + builder + "\"" : "\"" + builder + "\"";
        return true;
    }

    private static string Unique(HashSet<string> used, params string[] candidates)
    {
        foreach (string candidate in candidates.Where(c => !used.Contains(c)))
        {
            return candidate;
        }

        string baseName = candidates[0];
        for (int i = 2; ; i++)
        {
            string name = baseName + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (!used.Contains(name))
            {
                return name;
            }
        }
    }

    private static string Shorten(string text)
    {
        string single = string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return single.Length <= 60 ? single : single[..57] + "...";
    }
}
