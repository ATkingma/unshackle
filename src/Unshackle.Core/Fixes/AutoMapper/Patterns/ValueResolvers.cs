using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// The resolver forms of ForMember(d => d.X, o => o.MapFrom...) in one CreateMap: MapFrom&lt;TResolver&gt;(), MapFrom(new TResolver()),
/// MapFrom&lt;TResolver, TSourceMember&gt;(s => s.Y) and the lambdas MapFrom((s, d) => expr) / MapFrom((s, d, m, ctx) => expr).
/// Measured on AutoMapper 14 (TAAK-254): a resolver gets the destination object being built (its initializers ran, members configured
/// earlier are already set), the member's current value and a ResolutionContext; MapFrom&lt;TResolver&gt;() creates a new resolver per
/// Map call, MapFrom(instance) reuses the instance; an exception (also a NullReferenceException in a lambda or in the source member
/// expression) is wrapped in an AutoMapperMappingException, nothing is null-safe; TDestMember of a resolver is the member type itself,
/// the result of a lambda gets the built-in conversions. So only resolvers and lambdas that ignore destination, member and context move:
/// X = new TResolver().Resolve(source, default!, default!, default!) or X = expr. The resolver class stays as it is (it implements
/// AutoMapper's interface until the package goes, like the emptied profile).
/// </summary>
public class ValueResolvers
{
    private const string AutoMapperAssembly = "AutoMapper";

    private readonly IReadOnlyDictionary<string, InvocationExpressionSyntax> _calls;
    private readonly SemanticModel _model;
    private readonly int _position;
    private readonly INamedTypeSymbol? _profileType;

    // calls: destination member → its MapFrom call; position: where the generated class goes.
    public ValueResolvers(IReadOnlyDictionary<string, InvocationExpressionSyntax> calls, SemanticModel model, int position, INamedTypeSymbol? profileType)
    {
        _calls = calls;
        _model = model;
        _position = position;
        _profileType = profileType;
    }

    public IEnumerable<string> Members => _calls.Keys;

    public bool Has(string member) => _calls.ContainsKey(member);

    // Syntactic: X and the MapFrom call of ForMember(d => d.X, o => o.MapFrom...) in a resolver form, null for any other form.
    internal static (string Member, InvocationExpressionSyntax MapFrom)? Option(InvocationExpressionSyntax forMember)
    {
        if (forMember.ArgumentList.Arguments is not [
            { Expression: SimpleLambdaExpressionSyntax { Parameter.Identifier.ValueText: { } d, Body: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target, Name: { } name } } },
            { Expression: SimpleLambdaExpressionSyntax { Parameter.Identifier.ValueText: { } o, Body: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax options, Name: { } method } } call } }]
            || target.Identifier.ValueText != d || options.Identifier.ValueText != o || method.Identifier.ValueText != "MapFrom")
        {
            return null;
        }

        bool resolver = (method, call.ArgumentList.Arguments) switch
        {
            (GenericNameSyntax { TypeArgumentList.Arguments.Count: 1 }, []) => true,
            (GenericNameSyntax { TypeArgumentList.Arguments.Count: 2 }, [{ Expression: SimpleLambdaExpressionSyntax }]) => true,
            (IdentifierNameSyntax, [{ Expression: ParenthesizedLambdaExpressionSyntax or BaseObjectCreationExpressionSyntax }]) => true,
            _ => false,
        };
        return resolver ? (name.Identifier.ValueText, call) : null;
    }

    // Syntactic: whether a ForMember of the profile uses a resolver form (ValueResolverPattern claims the profile).
    internal static bool UsesResolver(ClassDeclarationSyntax profile) =>
        profile.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i =>
            i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ForMember" } && Option(i) is not null);

    /// <summary>
    /// Why the resolver of member (of type to, in a map to destination) cannot become X = value, or null with value; copies: a nested map
    /// or collection copy (see <see cref="MemberValues"/>).
    /// </summary>
    public string? Problem(string member, ITypeSymbol to, ITypeSymbol destination, MemberValues values, out string value, out bool copies)
    {
        value = "";
        copies = false;
        InvocationExpressionSyntax call = _calls[member];
        if (_model.GetSymbolInfo(call).Symbol is not IMethodSymbol { Name: "MapFrom", ContainingAssembly.Name: AutoMapperAssembly } method)
        {
            return $"MapFrom for {member} is not AutoMapper's";
        }

        ITypeSymbol? result;
        bool lambda = false;
        switch (call.ArgumentList.Arguments)
        {
            case [] when method.TypeArguments is [{ } resolver]:
                if (Resolver(member, resolver, null, out value, out result) is string problem)
                {
                    return problem;
                }

                break;
            case [{ Expression: SimpleLambdaExpressionSyntax sourceMember }] when method.TypeArguments is [{ } resolver, _]:
                if (Lambda(member, sourceMember, sourceMember.Parameter, [], out string? memberValue) is string memberProblem)
                {
                    return memberProblem;
                }

                if (Resolver(member, resolver, memberValue, out value, out result) is string resolverProblem)
                {
                    return resolverProblem;
                }

                break;
            case [{ Expression: BaseObjectCreationExpressionSyntax creation }]:
                if (creation.ArgumentList is { Arguments.Count: > 0 } || creation.Initializer is not null || _model.GetTypeInfo(creation).Type is not { } created)
                {
                    return $"MapFrom for {member} creates its resolver with arguments or an initializer";
                }

                if (Resolver(member, created, null, out value, out result) is string instanceProblem)
                {
                    return instanceProblem;
                }

                break;
            case [{ Expression: ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [{ } source, ..] } resolving }]:
                if (Lambda(member, resolving, source, resolving.ParameterList.Parameters.Skip(1), out string? text) is string lambdaProblem)
                {
                    return lambdaProblem;
                }

                value = text!;
                result = _model.GetTypeInfo(resolving.Body).Type;
                lambda = true;
                break;
            default:
                return $"MapFrom for {member} is a resolver form this fix does not know";
        }

        if (result is null || result.TypeKind == TypeKind.Error)
        {
            return $"MapFrom for {member} has no type of its own (a null literal, for one)";
        }

        if (values.Problem("MapFrom for " + member, value, result, to, destination, out string? mapped, out copies) is string valueProblem)
        {
            return valueProblem;
        }

        if (mapped is not null)
        {
            value = mapped;
            return null;
        }

        // Only a lambda's result can differ from the member type (a resolver's TDestMember is the member type itself).
        return lambda && _model.Compilation.ClassifyConversion(result, to) is { IsImplicit: true } conversion && (conversion.IsNumeric || conversion.IsNullable)
            ? null
            : $"MapFrom for {member} is {result.Name} but {member} is {to.Name} (AutoMapper converts){MemberValues.NoMap(result, to)}";
    }

    // new R().Resolve(source, default!, [sourceMember, ]default!, default!) for a resolver class that the generated code can create the
    // same way AutoMapper does and whose Resolve only reads source (and the source member).
    private string? Resolver(string member, ITypeSymbol type, string? sourceMember, out string value, out ITypeSymbol? result)
    {
        value = "";
        result = null;
        string kind = sourceMember is null ? "IValueResolver" : "IMemberValueResolver";
        if (type is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false, IsGenericType: false } named)
        {
            return $"the resolver {type.Name} for {member} is not a plain class";
        }

        if (named.InstanceConstructors.Any(c => c.Parameters.Length > 0))
        {
            return $"the resolver {named.Name} for {member} takes constructor parameters (AutoMapper gets it from the DI container); call its logic by hand";
        }

        if (named.BaseType?.SpecialType != SpecialType.System_Object
            || named.InstanceConstructors is not [{ DeclaredAccessibility: Accessibility.Public or Accessibility.Internal } constructor]
            || !(constructor.IsImplicitlyDeclared || constructor.DeclaringSyntaxReferences.All(r => r.GetSyntax() is ConstructorDeclarationSyntax { Initializer: null, Body.Statements.Count: 0 })))
        {
            return $"the resolver {named.Name} for {member} has a base class or a constructor that does work";
        }

        if (named.GetMembers().OfType<IFieldSymbol>().Any(f => !f.IsStatic && !f.IsConst))
        {
            return $"the resolver {named.Name} for {member} keeps state in fields (AutoMapper shares one instance per Map call or per configuration)";
        }

        if (CreateMapPattern.Weakest(CreateMapPattern.Effective(named), Accessibility.Public) is not (Accessibility.Public or Accessibility.Internal))
        {
            return $"the resolver {named.Name} for {member} is private or protected";
        }

        if (named.GetMembers("Resolve") is not [IMethodSymbol { IsStatic: false, MethodKind: MethodKind.Ordinary, DeclaredAccessibility: Accessibility.Public or Accessibility.Internal } resolve]
            || !named.AllInterfaces.Any(i => i.Name == kind && i.ContainingAssembly?.Name == AutoMapperAssembly
                && i.GetMembers("Resolve").Any(m => SymbolEqualityComparer.Default.Equals(named.FindImplementationForInterfaceMember(m), resolve))))
        {
            return $"the resolver {named.Name} for {member} implements {kind}.Resolve explicitly or overloads Resolve";
        }

        if (resolve.DeclaringSyntaxReferences is not [{ } reference] || reference.GetSyntax() is not MethodDeclarationSyntax declaration
            || !_model.Compilation.ContainsSyntaxTree(declaration.SyntaxTree))
        {
            return $"the source of the resolver {named.Name} for {member} is not in this project";
        }

        // destination (the object being built), the member's current value and the context: the generated code has none of them.
        IParameterSymbol[] unused = [resolve.Parameters[1], resolve.Parameters[^2], resolve.Parameters[^1]];
        SemanticModel resolverModel = _model.Compilation.GetSemanticModel(declaration.SyntaxTree);
        SyntaxNode? body = (SyntaxNode?)declaration.Body ?? declaration.ExpressionBody;
        foreach (IdentifierNameSyntax name in body?.DescendantNodes().OfType<IdentifierNameSyntax>() ?? [])
        {
            if (resolverModel.GetSymbolInfo(name).Symbol is IParameterSymbol parameter && unused.Contains(parameter, SymbolEqualityComparer.Default))
            {
                return $"the resolver {named.Name} for {member} uses {parameter.Name} ({Meaning(Array.IndexOf(unused, parameter))}), which a mapping method does not have";
            }
        }

        string arguments = sourceMember is null ? "source, default!, default!, default!" : $"source, default!, {sourceMember}, default!, default!";
        value = $"new {named.ToMinimalDisplayString(_model, _position)}().Resolve({arguments})";
        result = resolve.ReturnType;
        return null;
    }

    private static string Meaning(int index) => index switch
    {
        0 => "the destination object AutoMapper is building",
        1 => "the member's current value",
        _ => "AutoMapper's ResolutionContext",
    };

    // Why the expression body of lambda cannot move into the mapping method with source for its parameter source, or null with its text;
    // the other parameters (destination, member, context) must not be used.
    private string? Lambda(string member, LambdaExpressionSyntax lambda, ParameterSyntax source, IEnumerable<ParameterSyntax> others, out string? text)
    {
        text = null;
        if (lambda.Body is not ExpressionSyntax body || lambda.AsyncKeyword != default || _model.GetDeclaredSymbol(source) is not IParameterSymbol parameter)
        {
            return $"MapFrom for {member} is not a lambda with an expression body";
        }

        List<IParameterSymbol> unused = [.. others.Select(p => _model.GetDeclaredSymbol(p)).OfType<IParameterSymbol>()];
        List<SimpleNameSyntax> uses = [];
        foreach (SyntaxNode node in body.DescendantNodesAndSelf())
        {
            if (node is ThisExpressionSyntax or BaseExpressionSyntax)
            {
                return $"MapFrom for {member} uses this or base of the profile";
            }

            if (node is not SimpleNameSyntax name || !CreateMapPattern.IsReference(name))
            {
                continue;
            }

            ISymbol? symbol = _model.GetSymbolInfo(name).Symbol;
            if (SymbolEqualityComparer.Default.Equals(symbol, parameter))
            {
                uses.Add(name);
            }
            else if (symbol is IParameterSymbol other && unused.Contains(other, SymbolEqualityComparer.Default))
            {
                return $"MapFrom for {member} uses {other.Name} ({Meaning(unused.IndexOf(other))}), which a mapping method does not have";
            }
            else if (name.Identifier.ValueText == "source" && parameter.Name != "source" && symbol is not INamedTypeSymbol)
            {
                return $"MapFrom for {member} already uses the name source for something else";
            }
            else if (!CreateMapPattern.IsAvailableOutsideProfile(symbol, body, _profileType)
                || !_model.Compilation.IsSymbolAccessibleWithin(symbol is IMethodSymbol { ReducedFrom: { } extension } ? extension : symbol!, _model.Compilation.Assembly))
            {
                return $"MapFrom for {member} uses {name.Identifier.ValueText}, which is not available outside the profile";
            }
            else if (symbol is not (ILocalSymbol or IParameterSymbol or IRangeVariableSymbol) && !BindsTheSame(name, symbol!))
            {
                return $"MapFrom for {member} uses {name.Identifier.ValueText}, which means something else outside the type around the profile";
            }
        }

        ExpressionSyntax replaced = parameter.Name == "source"
            ? body
            : body.ReplaceNodes(uses, (original, _) => SyntaxFactory.IdentifierName("source").WithTriviaFrom(original));
        text = replaced.WithoutTrivia().ToString();
        return null;
    }

    // Whether a name that starts a lookup means the same symbol next to the outermost type.
    private bool BindsTheSame(SimpleNameSyntax name, ISymbol symbol)
    {
        if ((name.Parent is QualifiedNameSyntax { Right: var right } && right == name) || (name.Parent is AliasQualifiedNameSyntax { Name: var aliased } && aliased == name))
        {
            return true;
        }

        SymbolInfo there = _model.GetSpeculativeSymbolInfo(_position, name,
            SyntaxFacts.IsInNamespaceOrTypeContext(name) ? SpeculativeBindingOption.BindAsTypeOrNamespace : SpeculativeBindingOption.BindAsExpression);
        return there.CandidateSymbols.Append(there.Symbol).Any(s => SymbolEqualityComparer.Default.Equals(s?.OriginalDefinition, symbol.OriginalDefinition));
    }
}
