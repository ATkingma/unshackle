using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// A Profile whose constructor only holds CreateMap&lt;A, B&gt;() calls without options (a constant MemberList argument only steers
/// AssertConfigurationIsValid and is dropped) → a static class next to it with
/// one B ToB(this A source) per map, copying the members with the same name and type. Anything AutoMapper would do
/// beyond that (flattening, constructors) is Manual, except a member whose type pair has its own CreateMap in a profile this fix
/// rewrites (X = source.X?.ToY()), a list or array (copied into a new one, element by element through such a map), see <see cref="MemberValues"/>,
/// and AutoMapper's built-in conversions with a hand-written form that gives the same result (X = source.X.GetValueOrDefault(),
/// source.X.ToString(), Convert.ToInt32(source.X), (Color)source.X, ...), see <see cref="MemberConversions"/>.
/// The rewrite also understands
/// ForMember(d => d.X, o => o.Ignore()) (see <see cref="ForMemberIgnorePattern"/>) and ForMember(d => d.X, o => o.MapFrom(s => expr))
/// (see <see cref="ForMemberMapFromPattern"/>); those two claim the profiles that use them. With the solution, a map that a
/// mapper.Map(source, destination) call uses also gets B MapInto(this A source, B destination) (see <see cref="MapIntoPattern"/>).
/// </summary>
public class CreateMapPattern : ISolutionPattern
{
    private const string AutoMapperAssembly = "AutoMapper";

    public virtual string Id => "automapper.create-map";

    public virtual string Description =>
        "Profile with CreateMap<A, B>() without options → static B ToB(this A source) => new() { X = source.X, ... } for the members with the same name and type (or a built-in conversion AutoMapper does the same way); MapInto(this A source, B destination) when the solution calls Map(source, destination); AToBProjection when the solution calls ProjectTo; Map with options is Manual";

    // Map and ProjectTo calls that MapCallPattern does not rewrite are found too: while they run, a removed CreateMap fails at run time,
    // so they make the fix all-or-nothing.
    public virtual IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => n switch
        {
            ClassDeclarationSyntax c => IsProfileWithMaps(c, model) && !UsesForMember(c, ignore: true) && !UsesForMember(c, ignore: false),
            InvocationExpressionSyntax i => model.GetSymbolInfo(i) is var info
                && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "Map" or "ProjectTo" } method
                && method.ContainingAssembly?.Name == AutoMapperAssembly
                && !MapCallPattern.IsMapCall(i, model)
                && !MapIntoPattern.IsMapIntoCall(i, model)
                && !ProjectToPattern.IsProjectToCall(i, model),
            _ => false,
        });

    // Without the solution no MapInto is written: nothing says a Map(source, destination) call needs it.
    public virtual PatternResult Rewrite(SyntaxNode node, SemanticModel model) => Rewrite(node, model, new HashSet<string>(), nested: null, projectTo: new HashSet<string>());

    public virtual async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(solution);
        return node is ClassDeclarationSyntax
            ? Rewrite(node, model, await MapIntoPattern.TargetsAsync(solution, cancellationToken), await NestedMaps.FromAsync(model, solution, cancellationToken),
                await ProjectToPattern.TargetsAsync(solution, cancellationToken))
            : Rewrite(node, model, new HashSet<string>(), nested: null, projectTo: new HashSet<string>());
    }

    // The ToB methods of a profile, with nested maps of the whole solution; no MapInto (MapCallPattern only needs the method names).
    internal static async Task<PatternResult> RewriteMethodsAsync(ClassDeclarationSyntax profile, SemanticModel model, Solution solution, CancellationToken cancellationToken) =>
        Rewrite(profile, model, new HashSet<string>(), await NestedMaps.FromAsync(model, solution, cancellationToken), projectTo: new HashSet<string>());

    // mapInto: the maps (MapIntoPattern.Key) that get a MapInto next to ToB. nested: the maps a member can call (null: this project's).
    // projectTo: the maps that a ProjectTo call uses, which get an Expression<Func<A, B>> AToBProjection next to ToB (see Projections).
    private static PatternResult Rewrite(SyntaxNode node, SemanticModel model, IReadOnlySet<string> mapInto, NestedMaps? nested, IReadOnlySet<string> projectTo)
    {
        if (node is InvocationExpressionSyntax)
        {
            return PatternResult.Manual(
                "AutoMapper's ProjectTo and Map with options are not migrated yet (automapper.unsupported); while they run, the CreateMap rewrites of this fix stay undone.");
        }

        var profile = (ClassDeclarationSyntax)node;
        INamedTypeSymbol? profileType = model.GetDeclaredSymbol(profile);
        if (profile.Parent is not (BaseNamespaceDeclarationSyntax or CompilationUnitSyntax) || profile.TypeParameterList is not null
            || profile.Modifiers.Any(SyntaxKind.PartialKeyword)
            || profile.Members.OfType<ConstructorDeclarationSyntax>().ToList() is not [{ Body: { } body }])
        {
            return PatternResult.Manual($"Profile {profile.Identifier} is nested, generic, partial or has several constructors; write its mapping methods by hand.");
        }

        List<(TypeSyntax Source, TypeSyntax Destination, List<(string Name, string Value)> Members, bool MapInto)> maps = [];
        bool isInternal = false;
        HashSet<string> namespaces = new(StringComparer.Ordinal);
        NestedMaps allMaps = nested ?? NestedMaps.From(model);
        var values = new MemberValues(allMaps, model, profile.SpanStart, namespaces);
        Dictionary<string, (ITypeSymbol Source, ITypeSymbol Destination, IReadOnlyList<ProjectionMember> Members)> projectable = new(StringComparer.Ordinal);
        List<(TypeSyntax Source, TypeSyntax Destination, string Key, string Name)> projected = [];
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
            if (Problem(sourceType, destinationType, options, model, profileType, values, out List<(string Name, string Value)> members, out string? copied,
                out List<ProjectionMember> read) is string problem)
            {
                return PatternResult.Manual($"CreateMap<{sourceType.Name}, {destinationType.Name}>: {problem}; write the mapping method by hand.");
            }

            string key = MapIntoPattern.Key(sourceType, destinationType);
            projectable.TryAdd(key, (sourceType, destinationType, read));
            if (projectTo.Contains(key))
            {
                projected.Add((source, destination, key, Projections.Name(sourceType, destinationType)));
            }

            bool needsMapInto = mapInto.Contains(MapIntoPattern.Key(sourceType, destinationType));
            if (needsMapInto && copied is not null)
            {
                // Measured (TAAK-245): Map(source, destination) maps into the nested object or list the destination already has.
                return PatternResult.Manual($"CreateMap<{sourceType.Name}, {destinationType.Name}>: Map(source, destination) maps {copied} into the object or collection the destination already holds, MapInto would replace it; write MapInto by hand.");
            }

            Accessibility access = Weakest(Weakest(Effective(sourceType), Effective(destinationType)), Accessibility.Public);
            if (access is not (Accessibility.Public or Accessibility.Internal))
            {
                return PatternResult.Manual($"{sourceType.Name} or {destinationType.Name} is private or protected; an extension method cannot use it.");
            }

            isInternal |= access == Accessibility.Internal;
            maps.Add((source, destination, members, needsMapInto));
        }

        string className = profile.Identifier.ValueText + "Extensions";
        if (model.Compilation.GetSymbolsWithName(className, SymbolFilter.Type).Any())
        {
            return PatternResult.Manual($"A type {className} already exists; write the mapping methods of {profile.Identifier} by hand.");
        }

        // After all maps are known: a nested object or list of a later CreateMap is inlined too.
        var projections = new Projections(projectable, allMaps, model, profile.SpanStart);
        List<(TypeSyntax Source, TypeSyntax Destination, string Name, List<(string Name, string Value)> Members)> written = [];
        foreach ((TypeSyntax source, TypeSyntax destination, string key, string name) in projected)
        {
            if (written.Any(w => w.Name == name))
            {
                return PatternResult.Manual($"Two maps of {profile.Identifier} would both get a projection {name}; write them by hand.");
            }

            if (projections.Problem(key, out List<(string Name, string Value)> members) is string problem)
            {
                return PatternResult.Manual(problem + ".");
            }

            written.Add((source, destination, name, members));
            namespaces.UnionWith(["System", "System.Linq.Expressions"]);
            if (members.Any(m => m.Value.Contains(".Select(", StringComparison.Ordinal)))
            {
                namespaces.Add("System.Linq");
            }
        }

        return PatternResult.Fixed([
            profile.ReplaceNode(body, body.WithStatements([])),
            Extensions(profile, className, isInternal ? "internal" : "public", maps, written)], [.. namespaces.Order(StringComparer.Ordinal)]);
    }

    // Why the map cannot be written as plain assignments, or null with the destination members AutoMapper writes (and what to assign),
    // each having a source member of the same name and type (or one MemberValues maps) or a MapFrom expression. copied: the first member
    // that gets a new object or collection. read: what AutoMapper reads for each assigned member, before the ToB-only rewrites (a projection uses it).
    private static string? Problem(ITypeSymbol source, ITypeSymbol destination, Dictionary<string, SimpleLambdaExpressionSyntax?> options,
        SemanticModel model, INamedTypeSymbol? profileType, MemberValues values, out List<(string Name, string Value)> members, out string? copied,
        out List<ProjectionMember> read)
    {
        members = [];
        copied = null;
        read = [];
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
                if (MapFromProblem(target.Name, mapFromLambda, model, profileType, targetType, destination, values, out string value, out bool copies,
                    out string expression, out ITypeSymbol? expressionType) is string mapFromProblem)
                {
                    return mapFromProblem;
                }

                members.Add((target.Name, value));
                read.Add(new ProjectionMember { Name = target.Name, Value = expression, From = expressionType!, To = targetType });
                copied ??= copies ? target.Name : null;
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
            if (values.Problem(target.Name, "source." + target.Name, from, to, destination, out string? assigned, out bool copy) is string valueProblem)
            {
                return valueProblem;
            }

            if (assigned is null)
            {
                return $"{target.Name} is {from.Name} in {source.Name} but {to.Name} in {destination.Name} (AutoMapper converts){MemberValues.NoMap(from, to)}";
            }

            members.Add((target.Name, assigned));
            read.Add(new ProjectionMember { Name = target.Name, Value = "source." + target.Name, From = from, To = to });
            copied ??= copy ? target.Name : null;
        }

        return null;
    }

    // Why `s => expr` cannot move into the extension method as `X = expr`, or null with the text of expr (s becomes source).
    // AutoMapper runs a MapFrom expression null-safe (a NullReferenceException gives the default value) and converts types
    // where needed, a plain assignment does neither: so only expressions that cannot throw and need no conversion are moved.
    private static string? MapFromProblem(string member, SimpleLambdaExpressionSyntax lambda, SemanticModel model, INamedTypeSymbol? profileType,
        ITypeSymbol targetType, ITypeSymbol destination, MemberValues values, out string value, out bool copies, out string read, out ITypeSymbol? readType)
    {
        value = "";
        copies = false;
        read = "";
        readType = null;
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

        ExpressionSyntax replaced = parameterName == "source"
            ? body
            : body.ReplaceNodes(parameterUses, (original, _) => SyntaxFactory.IdentifierName("source").WithTriviaFrom(original));
        value = replaced.WithoutTrivia().ToString();
        read = value;
        readType = from;
        if (values.Problem("MapFrom for " + member, value, from, targetType, destination, out string? mapped, out copies) is string valueProblem)
        {
            return valueProblem;
        }

        if (mapped is not null)
        {
            value = mapped;
            return null;
        }

        if (!(model.Compilation.ClassifyConversion(from, targetType) is { IsImplicit: true } conversion && (conversion.IsNumeric || conversion.IsNullable)))
        {
            return $"MapFrom for {member} is {from.Name} but {member} is {targetType.Name} (AutoMapper converts){MemberValues.NoMap(from, targetType)}";
        }

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

    // `CreateMap<A, B>()` (or with a MemberList) or `CreateMap<A, B>().ForMember(d => d.X, o => o.Ignore())...`; anything else in the chain is no match.
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
        return expression.ArgumentList.Arguments is [] || (expression.ArgumentList.Arguments is [{ } memberList] && IsMemberList(memberList, model));
    }

    // CreateMap<A, B>(MemberList.X) or (memberList: MemberList.X): AutoMapper 14 reads the member list only in AssertConfigurationIsValid
    // (measured, TAAK-246), so the map is the same as without it. Only a constant: dropping any other expression could drop a side effect.
    private static bool IsMemberList(ArgumentSyntax argument, SemanticModel model) =>
        argument is { RefKindKeyword.RawKind: 0, NameColon: null or { Name.Identifier.ValueText: "memberList" } }
        && model.GetTypeInfo(argument.Expression).Type is INamedTypeSymbol { TypeKind: TypeKind.Enum, Name: "MemberList", ContainingAssembly.Name: AutoMapperAssembly }
        && model.GetConstantValue(argument.Expression).HasValue;

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

    internal static bool IsProfile(INamedTypeSymbol type)
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
    internal static Accessibility Effective(ITypeSymbol type) => type switch
    {
        IArrayTypeSymbol array => Effective(array.ElementType),
        INamedTypeSymbol named => named.TypeArguments.Aggregate(
            named.ContainingType is { } outer ? Weakest(named.DeclaredAccessibility, Effective(outer)) : named.DeclaredAccessibility,
            (access, argument) => Weakest(access, Effective(argument))),
        _ => Accessibility.Public,
    };

    internal static Accessibility Weakest(Accessibility a, Accessibility b) => (a, b) switch
    {
        (Accessibility.Public, _) => b,
        (_, Accessibility.Public) => a,
        (Accessibility.Internal, Accessibility.Internal) => Accessibility.Internal,
        _ => Accessibility.Private,
    };

    // ToB for CreateMap<A, B>: the last name of the destination type as written.
    internal static string MethodName(TypeSyntax destination) =>
        "To" + (destination is GenericNameSyntax generic ? generic.Identifier.ValueText : destination.ToString().Split('.')[^1]);

    // The static class goes right after the profile, with its indentation and line endings.
    private static MemberDeclarationSyntax Extensions(ClassDeclarationSyntax profile, string className, string access,
        List<(TypeSyntax Source, TypeSyntax Destination, List<(string Name, string Value)> Members, bool MapInto)> maps,
        List<(TypeSyntax Source, TypeSyntax Destination, string Name, List<(string Name, string Value)> Members)> projections)
    {
        string indent = profile.GetLeadingTrivia().LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString();
        string eol = profile.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString() is { Length: > 0 } found
            ? found
            : "\n";
        var text = new StringBuilder();
        text.Append(access).Append(" static class ").Append(className).Append(eol).Append(indent).Append('{').Append(eol);
        foreach ((TypeSyntax source, TypeSyntax destination, List<(string Name, string Value)> members, _) in maps)
        {
            if (text[^(eol.Length + 1)] != '{')
            {
                text.Append(eol);
            }

            text.Append(indent).Append("    ").Append(access).Append(" static ").Append(destination).Append(' ').Append(MethodName(destination))
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

        // After all ToB methods: MapCallPattern pairs the i-th method with the i-th CreateMap.
        foreach ((TypeSyntax source, TypeSyntax destination, List<(string Name, string Value)> members, _) in maps.Where(m => m.MapInto))
        {
            text.Append(eol).Append(indent).Append("    ").Append(access).Append(" static ").Append(destination)
                .Append(" MapInto(this ").Append(source).Append(" source, ").Append(destination).Append(" destination)").Append(eol)
                .Append(indent).Append("    {").Append(eol);
            foreach ((string member, string value) in members)
            {
                text.Append(indent).Append("        destination.").Append(member).Append(" = ").Append(value).Append(';').Append(eol);
            }

            text.Append(indent).Append("        return destination;").Append(eol).Append(indent).Append("    }").Append(eol);
        }

        // Properties, not methods: MapCallPattern counts only the ToB methods.
        foreach ((TypeSyntax source, TypeSyntax destination, string name, List<(string Name, string Value)> members) in projections)
        {
            text.Append(eol).Append(indent).Append("    ").Append(access).Append(" static Expression<Func<").Append(source).Append(", ").Append(destination)
                .Append(">> ").Append(name).Append(" => source => new ").Append(destination);
            if (members.Count == 0)
            {
                text.Append("();").Append(eol);
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
