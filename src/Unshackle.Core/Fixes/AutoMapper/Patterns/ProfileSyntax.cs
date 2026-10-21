using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// Syntax queries on AutoMapper profiles and MapperConfiguration lambdas for <see cref="CreateMapPattern"/> and its subclasses: which classes hold maps, their map statements, and parsing a CreateMap chain.
/// </summary>
internal static class ProfileSyntax
{
    // The statements of a profile constructor: the block's, or the expression of `=> CreateMap<A, B>()`.
    internal static IReadOnlyList<SyntaxNode> MapStatements(ConstructorDeclarationSyntax constructor) =>
        constructor.Body is { } body ? body.Statements : constructor.ExpressionBody is { } arrow ? [arrow] : [];

    // Find reports a nested profile at its outermost type: the rewrite replaces that type, so the extension class can go next to it.
    internal static SyntaxNode Anchor(SyntaxNode node) => node is ClassDeclarationSyntax c ? c.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().Last() : node;

    // A Profile with maps in a constructor, or (TAAK-286) a class whose `new MapperConfiguration(cfg => ...)` lambda holds CreateMap statements.
    internal static bool IsProfileWithMaps(ClassDeclarationSyntax c, SemanticModel model) =>
        model.GetDeclaredSymbol(c) is INamedTypeSymbol type
        && (IsProfile(type)
            ? c.Members.OfType<ConstructorDeclarationSyntax>().Any(ctor => ctor.Body is { Statements.Count: > 0 } || ctor.ExpressionBody is not null)
            : ConfigurationLambdas(c, model).Count > 0);

    // The lambdas of `new MapperConfiguration(cfg => ...)` / `new(cfg => ...)` in the class itself (not in a nested type) that call cfg.CreateMap<A, B>(). Syntax
    // first: this runs for every class of every file.
    internal static IReadOnlyList<LambdaExpressionSyntax> ConfigurationLambdas(ClassDeclarationSyntax c, SemanticModel model)
    {
        List<LambdaExpressionSyntax> found = [];
        foreach (BaseObjectCreationExpressionSyntax creation in c.DescendantNodes(n => n == c || n is not BaseTypeDeclarationSyntax).OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (creation.ArgumentList?.Arguments is [{ Expression: LambdaExpressionSyntax lambda }]
                && lambda.DescendantNodes().OfType<GenericNameSyntax>().Any(n => n.Identifier.ValueText == "CreateMap" && n.Parent is MemberAccessExpressionSyntax)
                && creation.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax } or PropertyDeclarationSyntax } // built once; a local or direct use is the configuration test's business
                && OnlyCreatesMaps(lambda)
                && ConfigurationSyntax.IsConfiguration(creation, model) && LambdaParameter(lambda, model) is not null)
            {
                found.Add(lambda);
            }
        }

        return found;
    }

    // Settings, AddProfile and the like on cfg stay as they are (Manual in ConfigurationTestPattern); only a lambda that does nothing but CreateMap is a map class.
    internal static bool OnlyCreatesMaps(LambdaExpressionSyntax lambda) =>
        (lambda switch { SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.ValueText, ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [{ } only] } => only.Identifier.ValueText, _ => null }) is { } name
        && lambda.DescendantNodes().OfType<IdentifierNameSyntax>().Where(i => i.Identifier.ValueText == name)
            .All(i => i.Parent is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "CreateMap" or "CreateProjection" } });

    // Whether the class builds a MapperConfiguration from a lambda, also one without maps left (a rewritten one).
    internal static bool HoldsConfiguration(ClassDeclarationSyntax c, SemanticModel model) =>
        c.DescendantNodes(n => n == c || n is not BaseTypeDeclarationSyntax).OfType<BaseObjectCreationExpressionSyntax>()
            .Any(creation => creation.ArgumentList?.Arguments is [{ Expression: LambdaExpressionSyntax }] && ConfigurationSyntax.IsConfiguration(creation, model));

    // cfg of `cfg => ...` (the one parameter of Action<IMapperConfigurationExpression>).
    internal static IParameterSymbol? LambdaParameter(LambdaExpressionSyntax lambda, SemanticModel model) => lambda switch
    {
        SimpleLambdaExpressionSyntax simple => model.GetDeclaredSymbol(simple.Parameter),
        ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [{ } only] } => model.GetDeclaredSymbol(only),
        _ => null,
    };

    // The statements of a MapperConfiguration lambda: its block's, or the expression of `cfg => cfg.CreateMap<A, B>()`.
    internal static IReadOnlyList<SyntaxNode> LambdaStatements(LambdaExpressionSyntax lambda) =>
        lambda.Body is BlockSyntax block ? block.Statements : lambda.Body is ExpressionSyntax expression ? [expression] : [];

    // The map statements of a class that IsProfileWithMaps: its Profile constructors', or its MapperConfiguration lambda's.
    internal static IReadOnlyList<SyntaxNode> MapStatements(ClassDeclarationSyntax profile, SemanticModel model) =>
        model.GetDeclaredSymbol(profile) is INamedTypeSymbol type && IsProfile(type)
            ? [.. profile.Members.OfType<ConstructorDeclarationSyntax>().SelectMany(MapStatements)]
            : [.. ConfigurationLambdas(profile, model).SelectMany(LambdaStatements)];

    // The `CreateMap<A, B>` name of a call: implicit in a Profile (parameter null), or on the parameter of the MapperConfiguration lambda.
    internal static SimpleNameSyntax? CreateMapName(InvocationExpressionSyntax call, IParameterSymbol? parameter, SemanticModel model) => call.Expression switch
    {
        GenericNameSyntax name when parameter is null => name,
        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax receiver, Name: GenericNameSyntax name }
            when parameter is not null && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(receiver).Symbol, parameter) => name,
        _ => null,
    };

    // Whether a CreateMap call is one of the map statements of its class (a Profile constructor's, or a MapperConfiguration lambda's), as opposed to a map elsewhere.
    internal static bool IsInMapStatements(InvocationExpressionSyntax createMap, ClassDeclarationSyntax profile, SemanticModel model)
    {
        if (model.GetDeclaredSymbol(profile) is INamedTypeSymbol type && IsProfile(type))
        {
            return createMap.Ancestors().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault() is ConstructorDeclarationSyntax
                && createMap.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().FirstOrDefault() is null;
        }

        return ConfigurationLambdas(profile, model).Any(l => l.Span.Contains(createMap.Span));
    }

    // Syntactic: whether a ForMember(..., o => o.Ignore()) (ignore) or ForMember(..., o => o.MapFrom(s => expr)) sits in the profile,
    // so exactly one pattern claims it: MapFrom before Ignore before plain CreateMap.
    internal static bool UsesForMember(ClassDeclarationSyntax profile, bool ignore) =>
        profile.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(i =>
            i.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ForMember" }
            && (ignore ? ForMemberOption(i) is { MapFrom: null } : IsMapFromCall(i)));

    // Any `o => o.MapFrom(...)`, also the forms that stay Manual (resolver lambda with two parameters, string name, generic): the MapFrom pattern reports them.
    internal static bool IsMapFromCall(InvocationExpressionSyntax forMember) =>
        forMember.ArgumentList.Arguments is [_, { Expression: SimpleLambdaExpressionSyntax { Body: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "MapFrom" } } } }];

    // `CreateMap<A, B>()` (or with a MemberList) or `CreateMap<A, B>().ForMember(d => d.X, o => o.Ignore())...`, optionally followed by
    // `.ReverseMap()` and ForMember options of the reverse map (reverse: those options, null without ReverseMap); anything else in the chain is no match.
    // statement: a statement of a block body, or the arrow clause of an expression body.
    internal static bool TryParseMap(SyntaxNode statement, SemanticModel model, [NotNullWhen(true)] out InvocationExpressionSyntax? createMap,
        [NotNullWhen(true)] out Dictionary<string, SimpleLambdaExpressionSyntax?>? options, out Dictionary<string, SimpleLambdaExpressionSyntax?>? reverse,
        out Dictionary<string, InvocationExpressionSyntax> resolvers, out InvocationExpressionSyntax? convertUsing, out Dictionary<string, InvocationExpressionSyntax> converters,
        out MemberOptions extra, out MemberOptions reverseExtra)
    {
        createMap = null;
        options = [];
        reverseExtra = new MemberOptions();
        reverse = null;
        extra = new MemberOptions();
        resolvers = [];
        convertUsing = null;
        converters = [];
        if (((statement as ExpressionStatementSyntax)?.Expression ?? (statement as ArrowExpressionClauseSyntax)?.Expression ?? statement as ExpressionSyntax) is not InvocationExpressionSyntax expression)
        {
            return false;
        }

        // CreateMap<A, B>().ConvertUsing(...): the converter replaces the whole map, so nothing else may be configured (Converters checks the form).
        if (expression is { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConvertUsing", Expression: InvocationExpressionSyntax typeMap } })
        {
            convertUsing = expression;
            createMap = typeMap;
            return typeMap.Expression is GenericNameSyntax or MemberAccessExpressionSyntax { Name: GenericNameSyntax }
                && (typeMap.ArgumentList.Arguments is [] || (typeMap.ArgumentList.Arguments is [{ } typeMapList] && IsMemberList(typeMapList, model)));
        }

        // The chain is read from the outside in: options seen before ReverseMap (outermost first) configure the reverse map.
        if (ReverseMapCall(expression, model) is { } reverseMap)
        {
            // A value converter has no reverse that is sure to be right: only plain options (and Condition/ForPath, TAAK-264) with ReverseMap.
            if (!TryParseOptions(ref expression, model, options, resolvers, converters, reverseExtra) || expression != reverseMap || converters.Count > 0)
            {
                extra.Unsupported = reverseExtra.Unsupported;
                return false;
            }

            reverse = options;
            options = [];
            expression = (InvocationExpressionSyntax)((MemberAccessExpressionSyntax)reverseMap.Expression).Expression;
        }

        if (!TryParseOptions(ref expression, model, options, resolvers, converters, extra) || (reverse is not null && converters.Count > 0))
        {
            return false;
        }

        createMap = expression;
        return expression.ArgumentList.Arguments is [] || (expression.ArgumentList.Arguments is [{ } memberList] && IsMemberList(memberList, model));
    }

    // The `.ReverseMap()` call of AutoMapper in the chain of expression, or null.
    internal static InvocationExpressionSyntax? ReverseMapCall(InvocationExpressionSyntax expression, SemanticModel model)
    {
        for (InvocationExpressionSyntax? call = expression; call is { Expression: MemberAccessExpressionSyntax access }; call = access.Expression as InvocationExpressionSyntax)
        {
            if (access is { Name.Identifier.ValueText: "ReverseMap", Expression: InvocationExpressionSyntax } && call.ArgumentList.Arguments.Count == 0
                && model.GetSymbolInfo(call).Symbol is IMethodSymbol { ContainingAssembly.Name: CreateMapPattern.AutoMapperAssembly })
            {
                return call;
            }
        }

        return null;
    }

    // Syntactic: whether the CreateMap call has `.ReverseMap()` in its chain (the profile rewrite then also writes A ToA(this B source)).
    internal static bool HasReverseMap(InvocationExpressionSyntax createMap)
    {
        for (SyntaxNode node = createMap; node.Parent is MemberAccessExpressionSyntax access && access.Expression == node && access.Parent is InvocationExpressionSyntax call; node = call)
        {
            if (access.Name.Identifier.ValueText == "ReverseMap" && call.ArgumentList.Arguments.Count == 0)
            {
                return true;
            }
        }

        return false;
    }

    // Strips `.ForMember(...)` calls off expression into options, until the call they hang on.
    // resolvers: the members a resolver form of MapFrom configures (see ValueResolvers), with that call.
    // converters: ForMember options with o.ConvertUsing(...) (TAAK-253).
    internal static bool TryParseOptions(ref InvocationExpressionSyntax expression, SemanticModel model, Dictionary<string, SimpleLambdaExpressionSyntax?> options,
        Dictionary<string, InvocationExpressionSyntax> resolvers, Dictionary<string, InvocationExpressionSyntax> converters, MemberOptions extra)
    {
        // Options without effect on the hand-written method (TAAK-257) are skipped; MaxDepth and PreserveReferences are checked in Rewrite.
        while (expression is { Expression: MemberAccessExpressionSyntax { Expression: InvocationExpressionSyntax skipped } access } forMember
            && (access.Name.Identifier.ValueText is "ForMember" or "ForPath" or "ForCtorParam" || UnsupportedOptions.IsDoNotValidate(forMember, model) || UnsupportedOptions.IsDepthOption(forMember, model)
                || IsConstructUsing(forMember, model)))
        {
            if (IsConstructUsing(forMember, model))
            {
                // Read in Problem (TAAK-266); a second one in the chain is no plain map.
                if (extra.ConstructUsing is not null)
                {
                    return false;
                }

                extra.ConstructUsing = forMember;
                expression = skipped;
                continue;
            }

            if (access.Name.Identifier.ValueText == "ForCtorParam")
            {
                // Read in ConstructorProblem (TAAK-270); any other form of it is no plain map.
                if (!extra.TryParseCtorParam(forMember, model))
                {
                    return false;
                }

                expression = skipped;
                continue;
            }

            if (access.Name.Identifier.ValueText is not ("ForMember" or "ForPath"))
            {
                expression = skipped;
                continue;
            }

            if (Converters.MemberOption(forMember) is var (convertedMember, option)
                && model.GetSymbolInfo(forMember).Symbol is IMethodSymbol { ContainingAssembly.Name: CreateMapPattern.AutoMapperAssembly }
                && access.Expression is InvocationExpressionSyntax beforeConverter && !options.ContainsKey(convertedMember) && !resolvers.ContainsKey(convertedMember)
                && converters.TryAdd(convertedMember, option))
            {
                expression = beforeConverter;
                continue;
            }

            if (ValueResolvers.Option(forMember) is var (resolved, call))
            {
                if (model.GetSymbolInfo(forMember).Symbol is not IMethodSymbol { ContainingAssembly.Name: CreateMapPattern.AutoMapperAssembly }
                    || access.Expression is not InvocationExpressionSyntax resolvedInner || options.ContainsKey(resolved) || converters.ContainsKey(resolved) || !resolvers.TryAdd(resolved, call))
                {
                    return false;
                }

                expression = resolvedInner;
                continue;
            }

            // Condition, PreCondition and ForPath (TAAK-255).
            if (access.Name.Identifier.ValueText == "ForPath" || ForMemberOption(forMember) is null)
            {
                if (model.GetSymbolInfo(forMember).Symbol is not IMethodSymbol { ContainingAssembly.Name: CreateMapPattern.AutoMapperAssembly }
                    || access.Expression is not InvocationExpressionSyntax other || !extra.TryParse(forMember, model, options))
                {
                    return false;
                }

                expression = other;
                continue;
            }

            if (ForMemberOption(forMember) is not var (member, mapFrom) || resolvers.ContainsKey(member) || converters.ContainsKey(member)
                || model.GetSymbolInfo(forMember).Symbol is not IMethodSymbol { ContainingAssembly.Name: CreateMapPattern.AutoMapperAssembly }
                || (mapFrom is not null && model.GetSymbolInfo((InvocationExpressionSyntax)mapFrom.Parent!.Parent!.Parent!).Symbol is not IMethodSymbol { Name: "MapFrom", ContainingAssembly.Name: CreateMapPattern.AutoMapperAssembly })
                || access.Expression is not InvocationExpressionSyntax inner
                || !options.TryAdd(member, mapFrom))
            {
                return false;
            }

            expression = inner;
        }

        return true;
    }

    internal static bool IsConstructUsing(InvocationExpressionSyntax call, SemanticModel model) =>
        call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConstructUsing" }
        && model.GetSymbolInfo(call) is var info && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { ContainingAssembly.Name: CreateMapPattern.AutoMapperAssembly };

    // CreateMap<A, B>(MemberList.X) or (memberList: MemberList.X): AutoMapper 14 reads the member list only in AssertConfigurationIsValid
    // (measured, TAAK-246), so the map is the same as without it. Only a constant: dropping any other expression could drop a side effect.
    internal static bool IsMemberList(ArgumentSyntax argument, SemanticModel model) =>
        argument is { RefKindKeyword.RawKind: 0, NameColon: null or { Name.Identifier.ValueText: "memberList" } }
        && model.GetTypeInfo(argument.Expression).Type is INamedTypeSymbol { TypeKind: TypeKind.Enum, Name: "MemberList", ContainingAssembly.Name: CreateMapPattern.AutoMapperAssembly }
        && model.GetConstantValue(argument.Expression).HasValue;

    // The X of `.ForMember(d => d.X, o => o.Ignore())` (MapFrom null) or `.ForMember(d => d.X, o => o.MapFrom(s => expr))` (MapFrom the lambda),
    // null for any other form (string member name, other option, nested member, block lambda, generic MapFrom).
    internal static (string Member, SimpleLambdaExpressionSyntax? MapFrom)? ForMemberOption(InvocationExpressionSyntax forMember)
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
            if (current is { Name: "Profile", ContainingAssembly.Name: CreateMapPattern.AutoMapperAssembly })
            {
                return true;
            }
        }

        return false;
    }
}
