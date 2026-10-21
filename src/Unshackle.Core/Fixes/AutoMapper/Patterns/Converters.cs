using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// ConvertUsing in a profile the profile patterns rewrite (TAAK-253). Type level: CreateMap&lt;A, B&gt;().ConvertUsing(s =&gt; expr) gives
/// B ToB(this A source) =&gt; expr; ConvertUsing&lt;TConverter&gt;() or ConvertUsing(new TConverter()) with ITypeConverter&lt;A, B&gt; gives
/// =&gt; new TConverter().Convert(source, null, null). Member level: ForMember(d =&gt; d.X, o =&gt; o.ConvertUsing(converter, s =&gt; s.Y)) with
/// IValueConverter gives X = converter.Convert(source.Y, null). AutoMapper 14 calls every converter with a null source too (also for a member
/// or a collection element of that type pair), so the generated code calls it without a null check. Only converters that do not read the
/// destination or the ResolutionContext and that the generated code can create are written; the rest stays Manual.
/// </summary>
public class Converters
{
    private const string AutoMapperAssembly = "AutoMapper";

    /// <summary>The body of B ToB(this A source) =&gt; body for a CreateMap&lt;A, B&gt;().ConvertUsing(...), or why it cannot be written.</summary>
    public static string? TypeProblem(InvocationExpressionSyntax convertUsing, ITypeSymbol source, ITypeSymbol destination, string method, SemanticModel model,
        INamedTypeSymbol? profileType, out string body)
    {
        body = "";
        if (model.GetSymbolInfo(convertUsing).Symbol is not IMethodSymbol { Name: "ConvertUsing", ContainingAssembly.Name: AutoMapperAssembly } convert)
        {
            return "ConvertUsing is not AutoMapper's";
        }

        if (model.LookupSymbols(convertUsing.SpanStart, source, method).Any())
        {
            return $"{source.Name} already has a member {method}, so the extension method would not be called";
        }

        switch (convertUsing.ArgumentList.Arguments, convert.TypeArguments)
        {
            case ([{ Expression: LambdaExpressionSyntax lambda }], _):
                return LambdaProblem(lambda, model, profileType, out body);
            case ([], [{ } converterType]):
                if (CreationProblem(converterType, model, convertUsing.SpanStart, out string created) is string creation)
                {
                    return creation;
                }

                return ConvertCall(created, converterType, "ITypeConverter", [source, destination], ["source"],
                    model, convertUsing.SpanStart, out body);
            case ([{ Expression: { } instance }], _) when model.GetTypeInfo(instance).Type is { } converterType:
                if (InstanceProblem(instance, model, profileType, out string text) is string problem)
                {
                    return problem;
                }

                return ConvertCall(text, converterType, "ITypeConverter", [source, destination], ["source"],
                    model, convertUsing.SpanStart, out body);
            default:
                return $"ConvertUsing({convertUsing.ArgumentList.Arguments}) is not a lambda or a converter this fix writes";
        }
    }

    /// <summary>
    /// The X of ForMember(d =&gt; d.X, o =&gt; o.ConvertUsing(...)) with the ConvertUsing call, or null for any other form.
    /// </summary>
    public static (string Member, InvocationExpressionSyntax Option)? MemberOption(InvocationExpressionSyntax forMember) =>
        forMember.ArgumentList.Arguments is [
            { Expression: SimpleLambdaExpressionSyntax { Parameter.Identifier.ValueText: { } d, Body: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target, Name: { } name } } },
            { Expression: SimpleLambdaExpressionSyntax { Parameter.Identifier.ValueText: { } o, Body: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax options, Name.Identifier.ValueText: "ConvertUsing" } } call } }]
        && target.Identifier.ValueText == d && options.Identifier.ValueText == o
            ? (name.Identifier.ValueText, call)
            : null;

    /// <summary>
    /// What X = ... gets for ForMember(d =&gt; d.X, o =&gt; o.ConvertUsing(converter[, s =&gt; s.Y])) with an IValueConverter, or why it cannot be written.
    /// Without a source expression AutoMapper hands the converter the source member of the same name.
    /// </summary>
    public static string? MemberProblem(InvocationExpressionSyntax option, string member, ITypeSymbol source, SemanticModel model,
        INamedTypeSymbol? profileType, out string value)
    {
        value = "";
        if (model.GetSymbolInfo(option).Symbol is not IMethodSymbol { Name: "ConvertUsing", ContainingAssembly.Name: AutoMapperAssembly, ContainingType.TypeArguments: [.., { } memberType] } convert)
        {
            return $"ConvertUsing for {member} is not AutoMapper's";
        }

        // ConvertUsing<TValueConverter, TSourceMember>(...) or ConvertUsing<TSourceMember>(IValueConverter<TSourceMember, TMember> converter, ...).
        string converter;
        ITypeSymbol converterType;
        ITypeSymbol sourceMember;
        SeparatedSyntaxList<ArgumentSyntax> rest;
        if (convert.TypeArguments is [{ } valueConverter, { } generic])
        {
            if (CreationProblem(valueConverter, model, option.SpanStart, out converter) is string creation)
            {
                return creation;
            }

            (converterType, sourceMember, rest) = (valueConverter, generic, option.ArgumentList.Arguments);
        }
        else if (convert.TypeArguments is [{ } instanceSource] && option.ArgumentList.Arguments is [{ Expression: { } instance }, ..] && model.GetTypeInfo(instance).Type is { } instanceType)
        {
            if (InstanceProblem(instance, model, profileType, out converter) is string problem)
            {
                return problem;
            }

            (converterType, sourceMember, rest) = (instanceType, instanceSource, SyntaxFactory.SeparatedList(option.ArgumentList.Arguments.Skip(1)));
        }
        else
        {
            return $"ConvertUsing for {member} is not a value converter this fix writes";
        }

        string from;
        switch (rest)
        {
            case []:
                // AutoMapper reads the source member with the destination member's name.
                ISymbol? match = CreateMapPattern.AllMembers(source).FirstOrDefault(m => m.Name == member && m is IFieldSymbol or IPropertySymbol { IsIndexer: false, GetMethod.DeclaredAccessibility: Accessibility.Public });
                ITypeSymbol? matchType = match is IPropertySymbol p ? p.Type : (match as IFieldSymbol)?.Type;
                if (matchType is null || !SymbolEqualityComparer.Default.Equals(matchType, sourceMember))
                {
                    return $"ConvertUsing for {member} needs a {sourceMember.Name} member {member} in {source.Name}";
                }

                from = "source." + member;
                break;
            case [{ Expression: SimpleLambdaExpressionSyntax lambda }]:
                // Measured: AutoMapper hands the converter the value as it is, and this expression is not null-safe (unlike MapFrom).
                if (LambdaProblem(lambda, model, profileType, out from) is string sourceProblem)
                {
                    return $"{sourceProblem} (source of {member})";
                }

                if (!SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(lambda.Body).Type, sourceMember))
                {
                    return $"ConvertUsing for {member} reads a value that is not a {sourceMember.Name}";
                }

                break;
            default:
                return $"ConvertUsing for {member} names its source member with a string";
        }

        return ConvertCall(converter, converterType, "IValueConverter", [sourceMember, memberType], [from], model, option.SpanStart, out value);
    }

    // B ToB(this A source) => expr: the lambda's source parameter renamed to source; the destination and context parameters must be unused.
    private static string? LambdaProblem(LambdaExpressionSyntax lambda, SemanticModel model, INamedTypeSymbol? profileType, out string body)
    {
        body = "";
        if (lambda.ExpressionBody is not { } expression || lambda.AsyncKeyword != default)
        {
            return "ConvertUsing has a lambda with a block body";
        }

        List<IParameterSymbol> parameters = [.. (lambda switch
        {
            SimpleLambdaExpressionSyntax simple => [simple.Parameter],
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters,
            _ => SyntaxFactory.SeparatedList<ParameterSyntax>(),
        }).Select(p => model.GetDeclaredSymbol(p)).OfType<IParameterSymbol>()];
        if (parameters.Count == 0)
        {
            return "ConvertUsing has a lambda without parameters";
        }

        List<SimpleNameSyntax> sourceUses = [];
        foreach (SyntaxNode node in expression.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case ThisExpressionSyntax or BaseExpressionSyntax:
                    return "ConvertUsing uses this or base of the profile";
                case SimpleNameSyntax name when MapAccessibility.IsReference(name):
                    ISymbol? symbol = model.GetSymbolInfo(name).Symbol;
                    int index = parameters.FindIndex(p => SymbolEqualityComparer.Default.Equals(p, symbol));
                    if (index == 0)
                    {
                        sourceUses.Add(name);
                    }
                    else if (index > 0)
                    {
                        // AutoMapper hands over the existing destination (Map(source, destination)) and its ResolutionContext; a ToB method has neither.
                        return index == 1 ? "ConvertUsing reads the destination" : "ConvertUsing uses the ResolutionContext";
                    }
                    else if (name.Identifier.ValueText == "source" && parameters[0].Name != "source" && symbol is not INamedTypeSymbol)
                    {
                        return "ConvertUsing already uses the name source for something else";
                    }
                    else if (!MapAccessibility.IsAvailableOutsideProfile(symbol, expression, profileType))
                    {
                        return $"ConvertUsing uses {name.Identifier.ValueText}, which is not available outside the profile";
                    }

                    break;
            }
        }

        ExpressionSyntax replaced = parameters[0].Name == "source"
            ? expression
            : expression.ReplaceNodes(sourceUses, (original, _) => SyntaxFactory.IdentifierName("source").WithTriviaFrom(original));
        // A body over several lines keeps its layout relative to the line it starts on (the generated method re-indents it).
        string lineText = expression.SyntaxTree.GetText().Lines.GetLineFromPosition(expression.SpanStart).ToString();
        string lineIndent = lineText[..^lineText.TrimStart().Length];
        body = string.Join('\n', replaced.WithoutTrivia().ToString().Split('\n')
            .Select((line, i) => i == 0 ? line : line.StartsWith(lineIndent, StringComparison.Ordinal) ? line[lineIndent.Length..] : line.TrimStart()));
        return null;
    }

    // new TConverter() for a converter AutoMapper constructs itself (per map call); a constructor with parameters means a service provider fills them.
    private static string? CreationProblem(ITypeSymbol converter, SemanticModel model, int position, out string created)
    {
        created = "";
        if (converter is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false, IsStatic: false } named || !model.IsAccessible(position, named))
        {
            return $"the converter {converter.Name} is not a class the generated code can create";
        }

        if (named.InstanceConstructors.Any(c => c.Parameters.Length > 0) || !named.InstanceConstructors.Any(c => model.IsAccessible(position, c)))
        {
            return $"the converter {converter.Name} has constructor parameters (a service provider creates it) or no accessible parameterless constructor";
        }

        created = $"new {named.ToMinimalDisplayString(model, position)}()";
        return null;
    }

    // A converter instance the generated code can repeat: new C() without arguments, or a static field or property outside the profile.
    private static string? InstanceProblem(ExpressionSyntax instance, SemanticModel model, INamedTypeSymbol? profileType, out string text)
    {
        text = instance.WithoutTrivia().ToString();
        bool repeatable = instance switch
        {
            ObjectCreationExpressionSyntax { ArgumentList.Arguments.Count: 0, Initializer: null } => true,
            IdentifierNameSyntax or MemberAccessExpressionSyntax => model.GetSymbolInfo(instance).Symbol is IFieldSymbol { IsStatic: true } or IPropertySymbol { IsStatic: true }
                && MapAccessibility.IsAvailableOutsideProfile(model.GetSymbolInfo(instance).Symbol, instance, profileType),
            _ => false,
        };
        return repeatable ? null : $"the converter {text} is not new C() or a static field or property the generated code can use";
    }

    // converter.Convert(arguments..., [destination,] context): only when the Convert of the interface is implemented in source of this compilation,
    // implicitly, and does not read the destination or the ResolutionContext; the call must bind to that method.
    private static string? ConvertCall(string converter, ITypeSymbol converterType, string interfaceName, ITypeSymbol[] typeArguments, string[] arguments,
        SemanticModel model, int position, out string call)
    {
        call = "";
        INamedTypeSymbol? implemented = converterType.AllInterfaces.FirstOrDefault(i => i.Name == interfaceName && i.ContainingAssembly?.Name == AutoMapperAssembly
            && i.TypeArguments.SequenceEqual<ITypeSymbol>(typeArguments, SymbolEqualityComparer.Default));
        if (implemented?.GetMembers("Convert").OfType<IMethodSymbol>().FirstOrDefault() is not { } member
            || converterType.FindImplementationForInterfaceMember(member) is not IMethodSymbol convert)
        {
            return $"the converter {converterType.Name} does not implement {interfaceName}<{string.Join(", ", typeArguments.Select(t => t.Name))}>";
        }

        if (convert.MethodKind == MethodKind.ExplicitInterfaceImplementation || convert.DeclaredAccessibility != Accessibility.Public)
        {
            return $"{converterType.Name}.Convert implements {interfaceName} explicitly";
        }

        if (convert.DeclaringSyntaxReferences is not [{ } reference] || !model.Compilation.ContainsSyntaxTree(reference.SyntaxTree)
            || reference.GetSyntax() is not MethodDeclarationSyntax declaration)
        {
            return $"{converterType.Name}.Convert is not in this project, so this fix cannot check what it reads";
        }

        SemanticModel declarationModel = model.Compilation.GetSemanticModel(reference.SyntaxTree);
        foreach (IParameterSymbol unused in convert.Parameters.Skip(arguments.Length))
        {
            if (declaration.DescendantNodes().OfType<IdentifierNameSyntax>().Any(n => n.Identifier.ValueText == unused.Name
                && SymbolEqualityComparer.Default.Equals(declarationModel.GetSymbolInfo(n).Symbol, unused)))
            {
                return unused.Type.Name == "ResolutionContext"
                    ? $"{converterType.Name}.Convert uses the ResolutionContext"
                    : $"{converterType.Name}.Convert reads the destination";
            }
        }

        List<string> all = [.. convert.Parameters.Select((p, i) => i < arguments.Length ? arguments[i] : Null(p.Type, model, position))];

        // The call is checked with default values of the parameter types in place of the source expressions.
        string Probe(IEnumerable<string> values) => $"{converter}.Convert({string.Join(", ", values)})";
        List<string> probe = [.. convert.Parameters.Select((p, i) => i < arguments.Length ? $"default({p.Type.ToMinimalDisplayString(model, position)})" : all[i])];
        if (!Binds(Probe(probe), convert, model, position))
        {
            // Several Convert overloads: a typed default for the destination picks the right one.
            for (int i = arguments.Length; i < all.Count - 1; i++)
            {
                all[i] = probe[i] = $"default({convert.Parameters[i].Type.ToMinimalDisplayString(model, position)})";
            }

            if (!Binds(Probe(probe), convert, model, position))
            {
                return $"the call to {converterType.Name}.Convert is ambiguous";
            }
        }

        call = Probe(all);
        return null;
    }

    private static bool Binds(string text, IMethodSymbol convert, SemanticModel model, int position) =>
        SymbolEqualityComparer.Default.Equals(
            model.GetSpeculativeSymbolInfo(position, SyntaxFactory.ParseExpression(text), SpeculativeBindingOption.BindAsExpression).Symbol?.OriginalDefinition,
            convert.OriginalDefinition);

    // The value passed for a parameter the converter does not read: null (null! in a nullable context) or default for a value type.
    private static string Null(ITypeSymbol type, SemanticModel model, int position) =>
        type.IsValueType ? "default" : model.GetNullableContext(position).WarningsEnabled() ? "null!" : "null";
}
