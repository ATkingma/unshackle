using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// A Profile whose constructor only holds CreateMap&lt;A, B&gt;() calls without options → a static class next to it with
/// one B ToB(this A source) per map, copying the members with the same name and type. Anything AutoMapper would do
/// beyond that (conversion, flattening, collections, constructors) is Manual. The rewrite also understands
/// ForMember(d => d.X, o => o.Ignore()) (see <see cref="ForMemberIgnorePattern"/>) and ForMember(d => d.X, o => o.MapFrom(s => expr))
/// (see <see cref="ForMemberMapFromPattern"/>); those two claim the profiles that use them.
/// </summary>
public class CreateMapPattern : IPattern
{
    private const string AutoMapperAssembly = "AutoMapper";

    public virtual string Id => "automapper.create-map";

    public virtual string Description =>
        "Profile with CreateMap<A, B>() without options → static B ToB(this A source) => new() { X = source.X, ... } for the members with the same name and type; Map(source, destination), Map with options and ProjectTo are Manual";

    // Map and ProjectTo calls that MapCallPattern does not rewrite are found too: while they run, a removed CreateMap fails at run time,
    // so they make the fix all-or-nothing.
    public virtual IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => n switch
        {
            ClassDeclarationSyntax c => IsProfileWithMaps(c, model) && !UsesForMember(c, ignore: true) && !UsesForMember(c, ignore: false),
            InvocationExpressionSyntax i => model.GetSymbolInfo(i) is var info
                && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "Map" or "ProjectTo" } method
                && method.ContainingAssembly?.Name == AutoMapperAssembly
                && !MapCallPattern.IsMapCall(i, model),
            _ => false,
        });

    public virtual PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is InvocationExpressionSyntax)
        {
            return PatternResult.Manual(
                "AutoMapper's ProjectTo, Map(source, destination) and Map with options are not migrated yet (automapper.map-into, automapper.unsupported); while they run, the CreateMap rewrites of this fix stay undone.");
        }

        var profile = (ClassDeclarationSyntax)node;
        INamedTypeSymbol? profileType = model.GetDeclaredSymbol(profile);
        if (profile.Parent is not (BaseNamespaceDeclarationSyntax or CompilationUnitSyntax) || profile.TypeParameterList is not null
            || profile.Modifiers.Any(SyntaxKind.PartialKeyword)
            || profile.Members.OfType<ConstructorDeclarationSyntax>().ToList() is not [{ Body: { } body }])
        {
            return PatternResult.Manual($"Profile {profile.Identifier} is nested, generic, partial or has several constructors; write its mapping methods by hand.");
        }

        List<(TypeSyntax Source, TypeSyntax Destination, List<(string Name, string Value)> Members)> maps = [];
        bool isInternal = false;
        foreach (StatementSyntax statement in body.Statements)
        {
            if (!TryParseMap(statement, model, out InvocationExpressionSyntax? invocation, out Dictionary<string, SimpleLambdaExpressionSyntax?>? options)
                || invocation.Expression is not GenericNameSyntax { Identifier.ValueText: "CreateMap", TypeArgumentList.Arguments: [{ } source, { } destination] }
                || model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly } method)
            {
                return PatternResult.Manual($"'{string.Join(' ', statement.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))}' is not a CreateMap<A, B>() without options; migrate Profile {profile.Identifier} by hand.");
            }

            ITypeSymbol sourceType = method.TypeArguments[0];
            ITypeSymbol destinationType = method.TypeArguments[1];
            if (Problem(sourceType, destinationType, options, model, profileType, out List<(string Name, string Value)> members) is string problem)
            {
                return PatternResult.Manual($"CreateMap<{sourceType.Name}, {destinationType.Name}>: {problem}; write the mapping method by hand.");
            }

            Accessibility access = Weakest(Weakest(Effective(sourceType), Effective(destinationType)), Accessibility.Public);
            if (access is not (Accessibility.Public or Accessibility.Internal))
            {
                return PatternResult.Manual($"{sourceType.Name} or {destinationType.Name} is private or protected; an extension method cannot use it.");
            }

            isInternal |= access == Accessibility.Internal;
            maps.Add((source, destination, members));
        }

        string className = profile.Identifier.ValueText + "Extensions";
        if (model.Compilation.GetSymbolsWithName(className, SymbolFilter.Type).Any())
        {
            return PatternResult.Manual($"A type {className} already exists; write the mapping methods of {profile.Identifier} by hand.");
        }

        return PatternResult.Fixed([
            profile.ReplaceNode(body, body.WithStatements([])),
            Extensions(profile, className, isInternal ? "internal" : "public", maps)]);
    }

    // Why the map cannot be written as plain assignments, or null with the destination members AutoMapper writes (and what to assign),
    // each having a source member of the same name and type or a MapFrom expression.
    private static string? Problem(ITypeSymbol source, ITypeSymbol destination, Dictionary<string, SimpleLambdaExpressionSyntax?> options,
        SemanticModel model, INamedTypeSymbol? profileType, out List<(string Name, string Value)> members)
    {
        members = [];
        if (destination is not INamedTypeSymbol { IsAbstract: false } named || destination.TypeKind is not (TypeKind.Class or TypeKind.Struct)
            || (destination.TypeKind == TypeKind.Class && !named.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public)))
        {
            return $"{destination.Name} has no public parameterless constructor (AutoMapper would map constructor parameters)";
        }

        List<ISymbol> readable = [.. AllMembers(source).Where(m => m switch
        {
            IPropertySymbol p => !p.IsIndexer && p.GetMethod?.DeclaredAccessibility == Accessibility.Public,
            IFieldSymbol => true,
            _ => false,
        })];
        List<ISymbol> destinationMembers = [.. AllMembers(destination).Where(m => m is IPropertySymbol { IsIndexer: false } or IFieldSymbol { IsConst: false })];
        foreach ((string name, SimpleLambdaExpressionSyntax? mapFrom) in options)
        {
            ISymbol? configured = destinationMembers.FirstOrDefault(m => m.Name == name);
            if (configured is null)
            {
                // AutoMapper rejects an unknown member in the configuration; there is nothing sound to leave out.
                return $"ForMember configures {name}, which {destination.Name} does not have as a public property or field";
            }

            if (mapFrom is not null && !(configured is IFieldSymbol { IsReadOnly: false } || configured is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public }))
            {
                return $"ForMember maps {destination.Name}.{name}, which has no public setter";
            }
        }

        foreach (ISymbol target in destinationMembers.Where(m => m is IPropertySymbol or IFieldSymbol { IsReadOnly: false }))
        {
            // Ignore(): AutoMapper leaves the member alone, so the new object does too (also when the source has a member of that name).
            if (options.TryGetValue(target.Name, out SimpleLambdaExpressionSyntax? mapFromLambda))
            {
                if (mapFromLambda is null)
                {
                    continue;
                }

                ITypeSymbol targetType = target is IPropertySymbol property ? property.Type : ((IFieldSymbol)target).Type;
                if (MapFromProblem(target.Name, mapFromLambda, model, profileType, targetType, out string value) is string mapFromProblem)
                {
                    return mapFromProblem;
                }

                members.Add((target.Name, value));
                continue;
            }

            bool writable = target is IFieldSymbol || ((IPropertySymbol)target).SetMethod?.DeclaredAccessibility == Accessibility.Public;
            List<ISymbol> matches = [.. readable.Where(m => string.Equals(m.Name, target.Name, StringComparison.OrdinalIgnoreCase))];
            if (!writable)
            {
                // AutoMapper fills a get-only member when the source has it (an existing collection, for one).
                if (matches.Count > 0)
                {
                    return $"{destination.Name}.{target.Name} has no public setter but {source.Name} has the member";
                }

                continue;
            }

            if (matches is not [{ } match] || match.Name != target.Name)
            {
                // No member of that exact name: AutoMapper may flatten (Customer.Name → CustomerName), call GetX() or match case-insensitively.
                return $"{destination.Name}.{target.Name} has no member with the same name in {source.Name}";
            }

            ITypeSymbol from = match is IPropertySymbol fp ? fp.Type : ((IFieldSymbol)match).Type;
            ITypeSymbol to = target is IPropertySymbol tp ? tp.Type : ((IFieldSymbol)target).Type;
            if (!SymbolEqualityComparer.Default.Equals(from, to))
            {
                return $"{target.Name} is {from.Name} in {source.Name} but {to.Name} in {destination.Name} (AutoMapper converts)";
            }

            if (to.SpecialType != SpecialType.System_String && (to is IArrayTypeSymbol || to.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable)))
            {
                return $"{target.Name} is a collection (AutoMapper copies it into a new one)";
            }

            members.Add((target.Name, "source." + target.Name));
        }

        return null;
    }

    // Why `s => expr` cannot move into the extension method as `X = expr`, or null with the text of expr (s becomes source).
    // AutoMapper runs a MapFrom expression null-safe (a NullReferenceException gives the default value) and converts types
    // where needed, a plain assignment does neither: so only expressions that cannot throw and need no conversion are moved.
    private static string? MapFromProblem(string member, SimpleLambdaExpressionSyntax lambda, SemanticModel model, INamedTypeSymbol? profileType,
        ITypeSymbol targetType, out string value)
    {
        value = "";
        if (lambda.Body is not ExpressionSyntax body || model.GetDeclaredSymbol(lambda.Parameter) is not IParameterSymbol parameter || lambda.AsyncKeyword != default)
        {
            return $"MapFrom for {member} is not a lambda with an expression body";
        }

        string parameterName = parameter.Name;
        List<SimpleNameSyntax> parameterUses = [];
        foreach (SyntaxNode node in body.DescendantNodesAndSelf())
        {
            ExpressionSyntax? receiver = null;
            switch (node)
            {
                case ThisExpressionSyntax or BaseExpressionSyntax:
                    return $"MapFrom for {member} uses this or base of the profile";
                case SimpleNameSyntax name when IsReference(name):
                    ISymbol? symbol = model.GetSymbolInfo(name).Symbol;
                    if (SymbolEqualityComparer.Default.Equals(symbol, parameter))
                    {
                        parameterUses.Add(name);
                    }
                    else if (name.Identifier.ValueText == "source" && parameterName != "source" && symbol is not INamedTypeSymbol)
                    {
                        return $"MapFrom for {member} already uses the name source for something else";
                    }
                    else if (!IsAvailableOutsideProfile(symbol, body, profileType))
                    {
                        return $"MapFrom for {member} uses {name.Identifier.ValueText}, which is not available outside the profile";
                    }

                    break;
                case MemberAccessExpressionSyntax access:
                    receiver = access.Expression;
                    break;
                case ElementAccessExpressionSyntax element:
                    receiver = element.Expression;
                    break;
            }

            if (receiver is not null && MayBeNull(receiver, model, parameter))
            {
                return $"MapFrom for {member} dereferences {receiver.ToString().Trim()}, which may be null (AutoMapper returns the default value, a plain assignment throws)";
            }
        }

        ITypeSymbol? from = model.GetTypeInfo(body).Type;
        if (from is null || from.TypeKind == TypeKind.Error)
        {
            return $"MapFrom for {member} has no type of its own (a null literal, for one)";
        }

        if (!SymbolEqualityComparer.Default.Equals(from, targetType)
            && !(model.Compilation.ClassifyConversion(from, targetType) is { IsImplicit: true } conversion && (conversion.IsNumeric || conversion.IsNullable)))
        {
            return $"MapFrom for {member} is {from.Name} but {member} is {targetType.Name} (AutoMapper converts)";
        }

        if (targetType.SpecialType != SpecialType.System_String && (targetType is IArrayTypeSymbol || targetType.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable)))
        {
            return $"MapFrom for {member} is a collection (AutoMapper copies it into a new one)";
        }

        ExpressionSyntax replaced = parameterName == "source"
            ? body
            : body.ReplaceNodes(parameterUses, (original, _) => SyntaxFactory.IdentifierName("source").WithTriviaFrom(original));
        value = replaced.WithoutTrivia().ToString();
        return null;
    }

    // A name that is read (not the right side of a.b, an initializer member, a named argument or a property name of an anonymous type).
    private static bool IsReference(SimpleNameSyntax name) => name.Parent switch
    {
        MemberAccessExpressionSyntax access => access.Expression == name,
        MemberBindingExpressionSyntax => false,
        NameColonSyntax or NameEqualsSyntax => false,
        AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment => assignment.Left != name,
        _ => true,
    };

    private static bool IsAvailableOutsideProfile(ISymbol? symbol, ExpressionSyntax body, INamedTypeSymbol? profileType) => symbol switch
    {
        null => false,
        INamedTypeSymbol or INamespaceSymbol or ITypeParameterSymbol => true,
        ILocalSymbol or IParameterSymbol or IRangeVariableSymbol =>
            symbol.Locations.Any(l => l.SourceTree == body.SyntaxTree && body.Span.Contains(l.SourceSpan)),
        _ => symbol.IsStatic && !SymbolEqualityComparer.Default.Equals(symbol.ContainingType, profileType),
    };

    // Whether `receiver.Something` can throw: a reference type or Nullable<T> that is not the lambda parameter and not known to be non-null.
    private static bool MayBeNull(ExpressionSyntax receiver, SemanticModel model, IParameterSymbol parameter)
    {
        ISymbol? symbol = model.GetSymbolInfo(receiver).Symbol;
        if (SymbolEqualityComparer.Default.Equals(symbol, parameter) || symbol is INamedTypeSymbol or INamespaceSymbol)
        {
            return false;
        }

        TypeInfo info = model.GetTypeInfo(receiver);
        if (info.Type is not { } type || (!type.IsReferenceType && type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T))
        {
            return false;
        }

        return info.Nullability.FlowState != NullableFlowState.NotNull;
    }

    // Public instance members, the type's own first; a member hidden by a derived one counts once.
    private static IEnumerable<ISymbol> AllMembers(ITypeSymbol type)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        for (ITypeSymbol? current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (ISymbol member in current.GetMembers().Where(m => !m.IsStatic && !m.IsImplicitlyDeclared && m.DeclaredAccessibility == Accessibility.Public))
            {
                if (seen.Add(member.Name))
                {
                    yield return member;
                }
            }
        }
    }

    protected internal static bool IsProfileWithMaps(ClassDeclarationSyntax c, SemanticModel model) =>
        model.GetDeclaredSymbol(c) is INamedTypeSymbol type && IsProfile(type)
        && c.Members.OfType<ConstructorDeclarationSyntax>().Any(ctor => ctor.Body is { Statements.Count: > 0 } || ctor.ExpressionBody is not null);

    // Syntactic: whether a ForMember(..., o => o.Ignore()) (ignore) or ForMember(..., o => o.MapFrom(s => expr)) sits in the profile,
    // so exactly one pattern claims it: MapFrom before Ignore before plain CreateMap.
    protected static bool UsesForMember(ClassDeclarationSyntax profile, bool ignore) =>
        profile.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i =>
            i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ForMember" }
            && (ignore ? ForMemberOption(i) is { MapFrom: null } : IsMapFromCall(i)));

    // Any `o => o.MapFrom(...)`, also the forms that stay Manual (resolver lambda with two parameters, string name, generic): the MapFrom pattern reports them.
    private static bool IsMapFromCall(InvocationExpressionSyntax forMember) =>
        forMember.ArgumentList.Arguments is [_, { Expression: SimpleLambdaExpressionSyntax { Body: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "MapFrom" } } } }];

    // `CreateMap<A, B>()` or `CreateMap<A, B>().ForMember(d => d.X, o => o.Ignore())...`; anything else in the chain is no match.
    private static bool TryParseMap(StatementSyntax statement, SemanticModel model, [NotNullWhen(true)] out InvocationExpressionSyntax? createMap,
        [NotNullWhen(true)] out Dictionary<string, SimpleLambdaExpressionSyntax?>? options)
    {
        createMap = null;
        options = [];
        if (statement is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax expression })
        {
            return false;
        }

        while (expression is { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ForMember" } access } forMember)
        {
            if (ForMemberOption(forMember) is not var (member, mapFrom)
                || model.GetSymbolInfo(forMember).Symbol is not IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly }
                || (mapFrom is not null && model.GetSymbolInfo((InvocationExpressionSyntax)mapFrom.Parent!.Parent!.Parent!).Symbol is not IMethodSymbol { Name: "MapFrom", ContainingAssembly.Name: AutoMapperAssembly })
                || access.Expression is not InvocationExpressionSyntax inner
                || !options.TryAdd(member, mapFrom))
            {
                return false;
            }

            expression = inner;
        }

        createMap = expression;
        return expression.ArgumentList.Arguments.Count == 0;
    }

    // The X of `.ForMember(d => d.X, o => o.Ignore())` (MapFrom null) or `.ForMember(d => d.X, o => o.MapFrom(s => expr))` (MapFrom the lambda),
    // null for any other form (string member name, other option, nested member, block lambda, generic MapFrom).
    private static (string Member, SimpleLambdaExpressionSyntax? MapFrom)? ForMemberOption(InvocationExpressionSyntax forMember)
    {
        if (forMember.ArgumentList.Arguments is not [
            { Expression: SimpleLambdaExpressionSyntax { Parameter.Identifier.ValueText: { } d, Body: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax target, Name: { } name } } },
            { Expression: SimpleLambdaExpressionSyntax { Parameter.Identifier.ValueText: { } o, Body: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax options } access } call } }]
            || target.Identifier.ValueText != d || options.Identifier.ValueText != o)
        {
            return null;
        }

        return (access.Name, call.ArgumentList.Arguments) switch
        {
            (IdentifierNameSyntax { Identifier.ValueText: "Ignore" }, []) => (name.Identifier.ValueText, null),
            (IdentifierNameSyntax { Identifier.ValueText: "MapFrom" }, [{ Expression: SimpleLambdaExpressionSyntax { Body: ExpressionSyntax } lambda }]) => (name.Identifier.ValueText, lambda),
            _ => null,
        };
    }

    private static bool IsProfile(INamedTypeSymbol type)
    {
        for (INamedTypeSymbol? current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current is { Name: "Profile", ContainingAssembly.Name: AutoMapperAssembly })
            {
                return true;
            }
        }

        return false;
    }

    // The accessibility a type really has, through its containing types and type arguments.
    private static Accessibility Effective(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => Effective(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Aggregate(
            named.ContainingType is { } outer ? Weakest(named.DeclaredAccessibility, Effective(outer)) : named.DeclaredAccessibility,
            (access, argument) => Weakest(access, Effective(argument))),
        _ => Accessibility.Public,
    };

    private static Accessibility Weakest(Accessibility a, Accessibility b) => (a, b) switch
    {
        (Accessibility.Public, _) => b,
        (_, Accessibility.Public) => a,
        (Accessibility.Internal, Accessibility.Internal) => Accessibility.Internal,
        _ => Accessibility.Private,
    };

    // The static class goes right after the profile, with its indentation and line endings.
    private static MemberDeclarationSyntax Extensions(ClassDeclarationSyntax profile, string className, string access,
        List<(TypeSyntax Source, TypeSyntax Destination, List<(string Name, string Value)> Members)> maps)
    {
        string indent = profile.GetLeadingTrivia().LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString();
        string eol = profile.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } found
            ? found
            : "\n";
        var text = new StringBuilder();
        text.Append(access).Append(" static class ").Append(className).Append(eol).Append(indent).Append('{').Append(eol);
        foreach ((TypeSyntax source, TypeSyntax destination, List<(string Name, string Value)> members) in maps)
        {
            if (text[^(eol.Length + 1)] != '{')
            {
                text.Append(eol);
            }

            string name = destination is GenericNameSyntax generic ? generic.Identifier.ValueText : destination.ToString().Split('.')[^1];
            text.Append(indent).Append("    ").Append(access).Append(" static ").Append(destination).Append(" To").Append(name)
                .Append("(this ").Append(source).Append(" source) => new()");
            if (members.Count == 0)
            {
                text.Append(';').Append(eol);
                continue;
            }

            text.Append(eol).Append(indent).Append("    {").Append(eol);
            foreach ((string member, string value) in members)
            {
                text.Append(indent).Append("        ").Append(member).Append(" = ").Append(value).Append(',').Append(eol);
            }

            text.Append(indent).Append("    };").Append(eol);
        }

        text.Append(indent).Append('}').Append(eol);
        // The blank line before it is set as trivia: the parser would drop a leading line ending.
        return SyntaxFactory.ParseMemberDeclaration(text.ToString())!.WithLeadingTrivia(SyntaxFactory.EndOfLine(eol), SyntaxFactory.Whitespace(indent));
    }
}
