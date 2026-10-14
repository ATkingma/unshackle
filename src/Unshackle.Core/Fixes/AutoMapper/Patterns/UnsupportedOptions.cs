using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// The AutoMapper options <see cref="CreateMapPattern"/> does not migrate (automapper.unsupported, TAAK-257): which ones can simply be
/// dropped, and for the rest a Manual reason that names the API and what to write by hand.
/// Measured against AutoMapper 14.0.0 (TAAK-257): MaxDepth(n) and PreserveReferences() change nothing on a map without nested objects or
/// collections, and ForSourceMember(..., o => o.DoNotValidate()) only steers AssertConfigurationIsValid.
/// </summary>
internal static class UnsupportedOptions
{
    private const string AutoMapperAssembly = "AutoMapper";

    // Name → what to write by hand instead.
    private static readonly Dictionary<string, string> s_advice = new(StringComparer.Ordinal)
    {
        ["MaxDepth"] = "MaxDepth limits how deep AutoMapper follows nested maps; write ToB by hand and stop the recursion where MaxDepth did",
        ["PreserveReferences"] = "PreserveReferences makes AutoMapper map an object it already saw to the same destination; write ToB by hand with a dictionary of mapped objects if the graph has shared or cyclic references",
        ["AfterMap"] = "AfterMap runs code after the members are set; write ToB with a block body (var destination = new B { ... }; <the AfterMap code>; return destination;)",
        ["BeforeMap"] = "BeforeMap runs code before the members are set; write ToB with a block body that runs it first",
        ["Include"] = "Include makes this map also serve derived types; write ToB by hand with a type switch that calls the derived maps",
        ["IncludeBase"] = "IncludeBase inherits the options of the base map; write ToB by hand and repeat the base members (or call the base ToB)",
        ["IncludeAllDerived"] = "IncludeAllDerived makes this map serve every derived type; write ToB by hand with a type switch over the derived types",
        ["IncludeMembers"] = "IncludeMembers maps members of child objects as if they were on the source; write ToB by hand with source.Child.X",
        ["ForSourceMember"] = "ForSourceMember in a form other than ForSourceMember(s => s.X, o => o.DoNotValidate()); write ToB by hand",
        ["ForAllMembers"] = "ForAllMembers applies an option to every member; write ToB by hand with that option per member (a condition becomes an if or ?:)",
        ["ForAllOtherMembers"] = "ForAllOtherMembers applies an option to the remaining members; write ToB by hand",
        ["ForCtorParam"] = "ForCtorParam maps a constructor parameter; write ToB by hand with new B(...)",
        ["ConstructUsing"] = "ConstructUsing creates the destination itself; write ToB by hand starting from that expression",
        ["ProjectUsing"] = "ProjectUsing sets the projection; write the Select by hand",
        ["AddTransform"] = "AddTransform changes values of a type after mapping; apply the transformation in ToB by hand",
        ["ValidateMemberList"] = "ValidateMemberList only steers AssertConfigurationIsValid; write ToB by hand",
        ["DisableCtorValidation"] = "DisableCtorValidation only steers AssertConfigurationIsValid; write ToB by hand",
        ["IgnoreAllPropertiesWithAnInaccessibleSetter"] = "IgnoreAllPropertiesWithAnInaccessibleSetter leaves members without a public setter alone; write ToB by hand without them",
        ["IgnoreAllSourcePropertiesWithAnInaccessibleSetter"] = "IgnoreAllSourcePropertiesWithAnInaccessibleSetter only steers validation; write ToB by hand",
        ["As"] = "As redirects the map to another destination type; write ToB by hand returning that type",
        ["ConvertUsingFactory"] = "a type converter maps the pair; write ToB as that conversion",
        ["UseDestinationValue"] = "UseDestinationValue keeps the destination's existing object; write MapInto by hand",
        ["AllowNullCollections"] = "AllowNullCollections changes how null collections are mapped; write ToB by hand",
        ["AllowNullDestinationValues"] = "AllowNullDestinationValues changes how null values are mapped; write ToB by hand",
        ["ShouldMapProperty"] = "ShouldMapProperty changes which members AutoMapper maps; write ToB by hand with only those members",
        ["ShouldMapField"] = "ShouldMapField changes which members AutoMapper maps; write ToB by hand with only those members",
        ["ShouldMapMethod"] = "ShouldMapMethod changes which methods AutoMapper reads; write ToB by hand",
        ["SourceMemberNamingConvention"] = "a naming convention changes which members match by name; write ToB by hand with the matched members",
        ["DestinationMemberNamingConvention"] = "a naming convention changes which members match by name; write ToB by hand with the matched members",
        ["RecognizePrefixes"] = "RecognizePrefixes changes which members match by name; write ToB by hand with the matched members",
        ["RecognizePostfixes"] = "RecognizePostfixes changes which members match by name; write ToB by hand with the matched members",
        ["RecognizeDestinationPrefixes"] = "RecognizeDestinationPrefixes changes which members match by name; write ToB by hand with the matched members",
        ["RecognizeDestinationPostfixes"] = "RecognizeDestinationPostfixes changes which members match by name; write ToB by hand with the matched members",
        ["ReplaceMemberName"] = "ReplaceMemberName changes which members match by name; write ToB by hand with the matched members",
        ["ClearPrefixes"] = "ClearPrefixes changes which members match by name; write ToB by hand with the matched members",
        ["AddGlobalIgnore"] = "AddGlobalIgnore leaves members of that name alone in every map; write ToB by hand without them",
        ["CreateProjection"] = "CreateProjection only serves ProjectTo; write the Select by hand",
    };

    // MaxDepth(constant) or PreserveReferences(): dropped when the map copies no nested object or collection (see DepthOption).
    public static bool IsDepthOption(InvocationExpressionSyntax call, SemanticModel model) =>
        call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: var name }
        && IsAutoMapper(call, model)
        && ((name == "PreserveReferences" && call.ArgumentList.Arguments.Count == 0)
            || (name == "MaxDepth" && call.ArgumentList.Arguments is [{ NameColon: null, RefKindKeyword.RawKind: 0 } depth] && model.GetConstantValue(depth.Expression).HasValue));

    // ForSourceMember(s => s.X, o => o.DoNotValidate()): only AssertConfigurationIsValid reads it.
    public static bool IsDoNotValidate(InvocationExpressionSyntax call, SemanticModel model) =>
        call is { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ForSourceMember" } }
        && call.ArgumentList.Arguments is [{ Expression: SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax } },
            { Expression: SimpleLambdaExpressionSyntax { Parameter.Identifier.ValueText: var o, Body: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax options, Name.Identifier.ValueText: "DoNotValidate" }, ArgumentList.Arguments.Count: 0 } } }]
        && options.Identifier.ValueText == o
        && IsAutoMapper(call, model);

    // The MaxDepth or PreserveReferences call in the map statement, or null.
    public static string? DepthOption(SyntaxNode statement, SemanticModel model) =>
        Chain(statement).FirstOrDefault(i => IsDepthOption(i, model)) is { Expression: MemberAccessExpressionSyntax access }
            ? access.Name.Identifier.ValueText
            : null;

    // A Manual reason for a statement of a profile constructor that is no CreateMap the rewrite understands, naming the first
    // unsupported AutoMapper API in it; null when it names none (the caller keeps its generic reason).
    public static string? Reason(SyntaxNode statement, SemanticModel model)
    {
        foreach (InvocationExpressionSyntax call in Chain(statement))
        {
            string? name = call.Expression switch
            {
                MemberAccessExpressionSyntax { Name: var n } => n.Identifier.ValueText,
                SimpleNameSyntax n => n.Identifier.ValueText,
                _ => null,
            };
            if (name is not null && s_advice.TryGetValue(name, out string? advice) && IsAutoMapper(call, model))
            {
                return advice;
            }

            if (name is "ForMember" && IsAutoMapper(call, model) && ForMemberReason(call) is string forMember)
            {
                return forMember;
            }
        }

        // A profile setting written as an assignment (AllowNullCollections = true, SourceMemberNamingConvention = ...).
        if (statement is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: var left } }
            && model.GetSymbolInfo(left).Symbol is IPropertySymbol { ContainingAssembly.Name: AutoMapperAssembly } property)
        {
            return s_advice.TryGetValue(property.Name, out string? setting)
                ? setting
                : $"the profile setting {property.Name} changes how AutoMapper maps; write ToB by hand";
        }

        return null;
    }

    // The calls of the statement's chain (CreateMap<A, B>().X().Y()), innermost first; not the calls inside their arguments.
    private static List<InvocationExpressionSyntax> Chain(SyntaxNode statement)
    {
        List<InvocationExpressionSyntax> chain = [];
        ExpressionSyntax? current = (statement as ExpressionStatementSyntax)?.Expression ?? (statement as ArrowExpressionClauseSyntax)?.Expression;
        while (current is InvocationExpressionSyntax call)
        {
            chain.Add(call);
            current = (call.Expression as MemberAccessExpressionSyntax)?.Expression;
        }

        chain.Reverse();
        return chain;
    }

    // ForMember with an option the rewrite does not read (Condition, NullSubstitute, a resolver, ...).
    // Null for Ignore, MapFrom, Condition, PreCondition and ConvertUsing: the rewrite reads those (or reports its own reason).
    private static string? ForMemberReason(InvocationExpressionSyntax forMember) =>
        forMember.ArgumentList.Arguments is [{ Expression: SimpleLambdaExpressionSyntax { Body: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax } } },
            { Expression: SimpleLambdaExpressionSyntax { Body: InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: var option } } call } }]
            ? (option, call.ArgumentList.Arguments) switch
            {
                ("Ignore", []) or ("Condition" or "PreCondition" or "MapFrom" or "ConvertUsing", _) => null,
                ("NullSubstitute", _) => "ForMember with NullSubstitute; write ToB by hand with ?? and the substitute",
                ("UseDestinationValue", _) => "ForMember with UseDestinationValue keeps the destination's existing value; write MapInto by hand",
                ("SetMappingOrder", _) => "ForMember with SetMappingOrder; write ToB by hand in that order",
                ("AllowNull" or "DoNotAllowNull", _) => $"ForMember with {option} changes the null handling; write that member by hand",
                _ => $"ForMember with {option}; write that member by hand",
            }
            : "ForMember in a form the rewrite does not read (string member name, nested member or block lambda); write that member by hand";

    private static bool IsAutoMapper(InvocationExpressionSyntax call, SemanticModel model) =>
        model.GetSymbolInfo(call) is var info
        && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { ContainingAssembly.Name: AutoMapperAssembly };
}
