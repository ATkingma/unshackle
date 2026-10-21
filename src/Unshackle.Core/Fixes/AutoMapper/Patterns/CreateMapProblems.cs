using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// Why a map of <see cref="CreateMapPattern"/> stays Manual, and the member values and constructor choice when it does not.
/// </summary>
internal static class CreateMapProblems
{
    // Why a MapFrom expression cannot move to the extension class next to the outermost type: it uses a private or protected member or
    // type (of the profile, a containing type or Profile itself), or a name that means something else there (a member of a containing type).
    internal static string? OutsideProblem(Dictionary<string, SimpleLambdaExpressionSyntax?> options, SemanticModel model, int position)
    {
        foreach ((string member, SimpleLambdaExpressionSyntax? lambda) in options)
        {
            if (lambda is not null && OutsideProblem("MapFrom for " + member, lambda.Body, model, position) is string problem)
            {
                return problem;
            }
        }

        return null;
    }

    // The same check for any expression that moves (what: "MapFrom for X", "ConstructUsing").
    internal static string? OutsideProblem(string what, SyntaxNode body, SemanticModel model, int position)
    {
        foreach (SimpleNameSyntax name in body.DescendantNodesAndSelf().OfType<SimpleNameSyntax>())
        {
            if (name.Parent is NameColonSyntax or NameEqualsSyntax || model.GetSymbolInfo(name).Symbol is not { } symbol
                || symbol is ILocalSymbol or IParameterSymbol or IRangeVariableSymbol)
            {
                continue;
            }

            if (!model.Compilation.IsSymbolAccessibleWithin(symbol is IMethodSymbol { ReducedFrom: { } extension } ? extension : symbol, model.Compilation.Assembly))
            {
                return $"{what} uses {name.Identifier.ValueText}, which is private or protected";
            }

            // Only a name that starts a lookup (not the right side of a.b or N.T) is bound again at the new place.
            if (!MapAccessibility.IsReference(name) || (name.Parent is QualifiedNameSyntax { Right: var right } && right == name)
                || (name.Parent is AliasQualifiedNameSyntax { Name: var aliased } && aliased == name))
            {
                continue;
            }

            SymbolInfo there = model.GetSpeculativeSymbolInfo(position, name,
                SyntaxFacts.IsInNamespaceOrTypeContext(name) ? SpeculativeBindingOption.BindAsTypeOrNamespace : SpeculativeBindingOption.BindAsExpression);
            if (!there.CandidateSymbols.Append(there.Symbol).Any(s => SymbolEqualityComparer.Default.Equals(s?.OriginalDefinition, symbol.OriginalDefinition)))
            {
                return $"{what} uses {name.Identifier.ValueText}, which means something else outside the type around the profile";
            }
        }

        return null;
    }

    // Why the map cannot be written as plain assignments, or null with the destination members AutoMapper writes (and what to assign),
    // each having a source member of the same name and type (or one MemberValues maps) or a MapFrom expression. copied: the first member
    // that gets a new object or collection. reversed (only for the reverse map of a ReverseMap): destination member → source member of a
    // reversed MapFrom; a destination member without any source is then left alone instead of Manual.
    internal static string? Problem(ITypeSymbol source, ITypeSymbol destination, Dictionary<string, SimpleLambdaExpressionSyntax?> options,
        SemanticModel model, INamedTypeSymbol? profileType, MemberValues values, out List<(string Name, string Value)> members, out string? copied, out string creation,
        Dictionary<string, string>? reversed = null, ValueResolvers? resolvers = null, List<ProjectionMember>? read = null,
        Dictionary<string, InvocationExpressionSyntax>? converters = null, MemberOptions? extra = null, bool asserted = false)
    {
        converters ??= [];
        // read: what AutoMapper reads for each assigned member, before the ToB-only rewrites (a projection uses it).
        members = [];
        copied = null;
        creation = "new()";
        if (destination is not INamedTypeSymbol { IsAbstract: false } named || destination.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return $"{destination.Name} has no public parameterless constructor (AutoMapper would map constructor parameters)";
        }

        List<ISymbol> readable = [.. CreateMapPattern.AllMembers(source).Where(m => m switch
        {
            IPropertySymbol p => !p.IsIndexer && p.GetMethod?.DeclaredAccessibility == Accessibility.Public,
            IFieldSymbol => true,
            _ => false,
        })];

        // ConstructUsing (TAAK-266) makes the destination through its own expression, so no constructor of the type is chosen.
        string? constructUsing = null;
        bool plainCreation = false;
        IMethodSymbol? constructor = null;
        if (extra?.ConstructUsing is { } constructCall)
        {
            if (ConstructUsingProblem(constructCall, destination, model, profileType, out constructUsing, out plainCreation) is string constructProblem)
            {
                return constructProblem;
            }
        }
        else if (destination.TypeKind == TypeKind.Class && ConstructorChoice(named, source, readable, model, extra?.CtorParams, out constructor) is string choiceProblem)
        {
            return choiceProblem;
        }
        List<ISymbol> destinationMembers = [.. CreateMapPattern.AllMembers(destination).Where(m => m is IPropertySymbol { IsIndexer: false } or IFieldSymbol { IsConst: false })];
        foreach ((string name, bool mapFrom) in options.Select(o => (o.Key, o.Value is not null)).Concat((resolvers?.Members ?? []).Select(m => (m, true))))
        {
            ISymbol? configured = destinationMembers.FirstOrDefault(m => m.Name == name);
            if (configured is null)
            {
                // AutoMapper rejects an unknown member in the configuration; there is nothing sound to leave out.
                return $"ForMember configures {name}, which {destination.Name} does not have as a public property or field";
            }

            if (mapFrom && !(configured is IFieldSymbol { IsReadOnly: false } || configured is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public }))
            {
                return $"ForMember maps {destination.Name}.{name}, which has no public setter";
            }
        }

        if (extra is { CtorParams.Count: > 0 } && constructor is null)
        {
            return $"ForCtorParam configures a constructor parameter, but {destination.Name} is made through ConstructUsing or its parameterless constructor";
        }

        if ((constructor is not null || constructUsing is not null) && extra is { Conditions.Count: > 0 } or { Paths.Count: > 0 })
        {
            return $"{destination.Name} is created through its constructor or ConstructUsing, which may set a member a Condition or ForPath keeps or fills (TAAK-255)";
        }

        if (converters.Keys.FirstOrDefault(name => !destinationMembers.Any(m => m.Name == name
            && (m is IFieldSymbol { IsReadOnly: false } || m is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public }))) is { } unwritable)
        {
            return $"ForMember converts {destination.Name}.{unwritable}, which is not a public property with a setter or a field";
        }

        if (constructor is not null && converters.Count > 0)
        {
            return $"{destination.Name} is made through a constructor and ForMember uses a value converter; write the mapping method by hand";
        }

        if (constructor is not null && ConstructorProblem(constructor, source, destination, options, readable, values, extra?.CtorParams, model, profileType, out creation, ref copied) is string constructorProblem)
        {
            return constructorProblem;
        }

        foreach (ISymbol target in destinationMembers.Where(m => m is IPropertySymbol or IFieldSymbol { IsReadOnly: false }))
        {
            // A member a constructor parameter of the same name (any case) filled is not mapped again (measured, TAAK-251).
            if (constructor is not null && constructor.Parameters.Any(p => string.Equals(p.Name, target.Name, StringComparison.OrdinalIgnoreCase)))
            {
                if (resolvers?.Has(target.Name) == true)
                {
                    return $"a value resolver configures {target.Name}, which a constructor parameter of {destination.Name} also fills";
                }

                continue;
            }

            if (resolvers?.Has(target.Name) == true)
            {
                ITypeSymbol resolvedType = target is IPropertySymbol resolvedProperty ? resolvedProperty.Type : ((IFieldSymbol)target).Type;
                if (resolvers.Problem(target.Name, resolvedType, destination, values, out string resolved, out bool resolvedCopies) is string resolverProblem)
                {
                    return resolverProblem;
                }

                members.Add((target.Name, resolved));
                copied ??= resolvedCopies ? target.Name : null;
                continue;
            }

            // ConvertUsing(converter[, s => s.Y]): X = converter.Convert(source.Y, null) (TAAK-253).
            if (converters.TryGetValue(target.Name, out InvocationExpressionSyntax? converter))
            {
                if (Converters.MemberProblem(converter, target.Name, source, model, profileType, out string converted) is string converterProblem)
                {
                    return converterProblem;
                }

                members.Add((target.Name, converted));
                continue;
            }

            if (extra is not null && extra.Paths.TryGetValue(target.Name, out List<(string Member, SimpleLambdaExpressionSyntax MapFrom)>? path))
            {
                if (PathProblem(target, path, source, options, extra, model, profileType, values, out string created) is string pathProblem)
                {
                    return pathProblem;
                }

                members.Add((target.Name, created));
                continue;
            }

            // Ignore(): AutoMapper leaves the member alone, so the new object does too (also when the source has a member of that name).
            if (options.TryGetValue(target.Name, out SimpleLambdaExpressionSyntax? mapFromLambda))
            {
                if (mapFromLambda is null)
                {
                    continue;
                }

                ITypeSymbol targetType = target is IPropertySymbol property ? property.Type : ((IFieldSymbol)target).Type;
                if (MapFromProblem(target.Name, mapFromLambda, model, profileType, targetType, destination, values, nullSafe: true, out string value, out bool copies,
                    out string expression, out ITypeSymbol? expressionType, out string? readProblem) is string mapFromProblem)
                {
                    return mapFromProblem;
                }

                members.Add((target.Name, value));
                read?.Add(new ProjectionMember { Name = target.Name, Value = expression, From = expressionType!, To = targetType, Problem = readProblem });
                copied ??= copies ? target.Name : null;
                continue;
            }

            bool writable = target is IFieldSymbol || ((IPropertySymbol)target).SetMethod?.DeclaredAccessibility == Accessibility.Public;
            if (reversed is not null && reversed.TryGetValue(target.Name, out string? reversedFrom))
            {
                // Measured (TAAK-249): the reversed MapFrom wins over a source member of the same name.
                if (!writable || readable.FirstOrDefault(m => m.Name == reversedFrom) is not { } reversedMember)
                {
                    return $"{destination.Name}.{target.Name} gets {source.Name}.{reversedFrom} from the reversed MapFrom, but cannot be written or read that way";
                }

                ITypeSymbol reversedType = reversedMember is IPropertySymbol rp ? rp.Type : ((IFieldSymbol)reversedMember).Type;
                ITypeSymbol targetMemberType = target is IPropertySymbol tmp ? tmp.Type : ((IFieldSymbol)target).Type;
                if (values.Problem(target.Name, "source." + reversedFrom, reversedType, targetMemberType, destination, out string? reversedValue, out bool reversedCopy) is string reversedProblem)
                {
                    return reversedProblem;
                }

                if (reversedValue is null)
                {
                    return $"{target.Name} is {targetMemberType.Name} in {destination.Name} but {reversedFrom} is {reversedType.Name} in {source.Name} (AutoMapper converts){MemberValues.NoMap(reversedType, targetMemberType)}";
                }

                members.Add((target.Name, reversedValue));
                copied ??= reversedCopy ? target.Name : null;
                continue;
            }

            List<ISymbol> matches = [.. readable.Where(m => string.Equals(m.Name, target.Name, StringComparison.OrdinalIgnoreCase))];
            if (reversed is not null && matches.Count == 0 && !MayFlatten(target.Name, source))
            {
                // The reverse map has MemberList.None: AutoMapper leaves a member without a source alone (measured, TAAK-249).
                continue;
            }

            if (!writable)
            {
                // Measured (TAAK-261): when the source has the member, AutoMapper writes a non-public setter and fills a get-only collection
                // (clears it, adds the source items); a get-only member of another type (an array, a string, an object) it leaves alone.
                if (MayFlatten(target.Name, source) && target is IPropertySymbol { SetMethod: not null })
                {
                    return $"{destination.Name}.{target.Name} has a non-public setter, which AutoMapper writes from {source.Name} through reflection; a mapping method cannot write it (principally manual unless the type gets a public setter)";
                }

                if (MayFlatten(target.Name, source) && IsFilledCollection(target is IPropertySymbol gp ? gp.Type : ((IFieldSymbol)target).Type))
                {
                    return $"{destination.Name}.{target.Name} is a get-only collection, which AutoMapper fills with the items from {source.Name}";
                }

                continue;
            }

            ITypeSymbol to = target is IPropertySymbol tp ? tp.Type : ((IFieldSymbol)target).Type;
            ITypeSymbol from;
            string reads;
            string plain;
            ITypeSymbol plainType;
            if (matches is [{ } match] && match.Name == target.Name)
            {
                from = match is IPropertySymbol fp ? fp.Type : ((IFieldSymbol)match).Type;
                reads = "source." + target.Name;
                plain = reads;
                plainType = from;
            }
            else if (reversed is not null)
            {
                return $"{destination.Name}.{target.Name} has no member with the same name in {source.Name}";
            }
            else if (Flattening.Problem(source, destination, target.Name, out IReadOnlyList<ISymbol> found) is null && found.Count == 0)
            {
                // Measured (TAAK-261): AutoMapper leaves a member without a source alone, so the new object keeps its initializer or default.
                // Only AssertConfigurationIsValid rejects that; when the solution calls it, the member must be mapped in a way this fix does not see.
                if (asserted)
                {
                    return $"{destination.Name}.{target.Name} has no member with the same name in {source.Name}, nor one AutoMapper flattens or reads through a Get method, "
                        + "yet the solution calls AssertConfigurationIsValid, which rejects such a member";
                }

                continue;
            }
            else if (FlattenedProblem(source, destination, target.Name, to, values, out reads, out from, out plain, out plainType) is string flattened)
            {
                // AutoMapper's other name rules (TAAK-256): any case, a Get prefix, flattening (Customer.Name → CustomerName).
                return flattened;
            }

            if (values.Problem(target.Name, reads, from, to, destination, out string? assigned, out bool copy) is string valueProblem)
            {
                return valueProblem;
            }

            if (assigned is null)
            {
                return $"{target.Name} is {from.Name} in {source.Name} but {to.Name} in {destination.Name} (AutoMapper converts){MemberValues.NoMap(from, to)}";
            }

            members.Add((target.Name, assigned));
            // ProjectTo reads a flattened path without null checks (TAAK-252: the query has none).
            read?.Add(new ProjectionMember { Name = target.Name, Value = plain, From = plainType, To = to });
            copied ??= copy ? target.Name : null;
        }

        // Measured (TAAK-282, AutoMapper 14.0.0): a required member nothing maps stays at its default (AutoMapper does not enforce `required`),
        // but the compiler rejects a creation that does not name it (CS9035). Naming it as default is the same only while the new object holds
        // the default there anyway.
        foreach (ISymbol target in destinationMembers.Where(m => m is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true, IsReadOnly: false }))
        {
            if (members.Any(m => m.Name == target.Name))
            {
                continue;
            }

            if (!KeepsDefault(target, destination))
            {
                return $"{destination.Name}.{target.Name} is required and nothing maps it, but it has an initializer or {destination.Name} has a constructor that may set it, which AutoMapper keeps; the creation must name it by hand";
            }

            ITypeSymbol type = target is IPropertySymbol requiredProperty ? requiredProperty.Type : ((IFieldSymbol)target).Type;
            string unset = values.NullableWarnings && type.IsReferenceType && type.NullableAnnotation != NullableAnnotation.Annotated ? "default!" : "default";
            members.Add((target.Name, unset));
            read?.Add(new ProjectionMember { Name = target.Name, Value = unset, From = type, To = type });
        }

        if (constructUsing is not null)
        {
            // The expression runs first, then the members are assigned: onto a plain `new T(...)` as its initializer, otherwise in a block body.
            creation = members.Count == 0 || plainCreation ? constructUsing : BlockMarker + constructUsing;
        }

        return extra is null ? null : ConditionProblem(destination, extra, model, profileType, values, members);
    }

    // Starts the creation text of a map that needs a block body (var destination = expression; destination.X = ...; return destination;).
    internal const char BlockMarker = '\u0001';

    // Measured (TAAK-266, AutoMapper 14.0.0): AutoMapper takes the constructor with the most parameters it can resolve (a parameter with a source member
    // of the same name in any case, flattened or read through a Get method, or an optional one), the first declared on a tie, over a parameterless one
    // and whatever its accessibility (private ones too); it throws when none can be resolved. Why that choice cannot be frozen into one new(...) call, or null
    // with the chosen constructor (null: the public parameterless one, `new()`).
    internal static string? ConstructorChoice(INamedTypeSymbol destination, ITypeSymbol source,
        List<ISymbol> readable, SemanticModel model, Dictionary<string, SimpleLambdaExpressionSyntax>? ctorParams, out IMethodSymbol? chosen)
    {
        chosen = null;
        // The record copy constructor does not count.
        List<IMethodSymbol> candidates = [.. destination.InstanceConstructors.Where(c => !(c.IsImplicitlyDeclared && c.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, destination)))];
        if (candidates is [{ DeclaredAccessibility: Accessibility.Public } only])
        {
            // The common case: its parameters are checked by ConstructorProblem.
            chosen = only.Parameters.Length == 0 ? null : only;
            return null;
        }

        if (candidates.Count == 0 || candidates is [var lone] && lone.DeclaredAccessibility != Accessibility.Public)
        {
            return $"{destination.Name} has no public constructor (AutoMapper uses the one with the most parameters it can resolve, also a private one)";
        }

        IMethodSymbol? winner = null;
        List<IMethodSymbol> sameCount = [];
        foreach (IMethodSymbol candidate in candidates.OrderByDescending(c => c.Parameters.Length))
        {
            if (winner is not null && candidate.Parameters.Length < winner.Parameters.Length)
            {
                break;
            }

            if (candidate.Parameters.FirstOrDefault(p => p.RefKind != RefKind.None || p.IsParams) is { } odd)
            {
                return $"a constructor of {destination.Name} has a ref, out or params parameter {odd.Name}";
            }

            bool resolvable = true;
            foreach (IParameterSymbol parameter in candidate.Parameters)
            {
                // A ForMember on the member of that name does not make the parameter resolvable (measured, TAAK-266).
                int matches = readable.Count(m => string.Equals(m.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
                if (matches > 1)
                {
                    return $"the constructor parameter {parameter.Name} of {destination.Name} matches several members of {source.Name}";
                }

                if (matches != 1 && ctorParams?.ContainsKey(parameter.Name) == true)
                {
                    return $"{destination.Name} has several constructors and ForCtorParam fills the parameter {parameter.Name}, which has no single member of that name in {source.Name}; which constructor AutoMapper picks is not decided here";
                }

                if (matches == 0 && MayFlatten(parameter.Name, source))
                {
                    return $"{destination.Name} has several constructors and AutoMapper may fill the parameter {parameter.Name} of one by flattening or a Get method of {source.Name}; which one it picks is not decided here";
                }

                resolvable &= matches == 1 || parameter.HasExplicitDefaultValue;
            }

            if (resolvable)
            {
                winner ??= candidate;
                sameCount.Add(candidate);
            }
        }

        if (winner is null)
        {
            return $"AutoMapper can resolve no constructor of {destination.Name} from {source.Name} (it throws)";
        }

        if (sameCount.Count > 1 && sameCount.Select(c => c.Locations.FirstOrDefault()?.SourceTree).Distinct().Count() > 1)
        {
            return $"{destination.Name} has several constructors with {winner.Parameters.Length} parameters AutoMapper can resolve, declared in different files (the first declared wins, which is not decided here)";
        }

        // sameCount holds them in the order AutoMapper tries (declaration order).
        winner = sameCount[0];
        if (winner.Parameters.Length == 0)
        {
            return winner.DeclaredAccessibility == Accessibility.Public
                ? null
                : $"AutoMapper would create {destination.Name} through its parameterless constructor, which is not public";
        }

        if (!model.Compilation.IsSymbolAccessibleWithin(winner, model.Compilation.Assembly))
        {
            return $"AutoMapper picks the constructor ({string.Join(", ", winner.Parameters.Select(p => p.Name))}) of {destination.Name}, which is private or protected; the extension method cannot call it";
        }

        chosen = winner;
        return null;
    }

    // Why `ConstructUsing(s => expression)` cannot become the creation of the new object, or null with that expression over the name source.
    // Measured (TAAK-266): the destination is made by the expression instead of a constructor of the type (also one the type has no parameterless
    // for), then every member is mapped as without it: a settable member is overwritten from its source member, one without a source keeps what the
    // expression gave it, Ignore leaves it, MapFrom wins, a get-only member is not written; the expression runs unguarded (a null inside throws).
    internal static string? ConstructUsingProblem(InvocationExpressionSyntax call, ITypeSymbol destination, SemanticModel model, INamedTypeSymbol? profileType,
        out string? text, out bool plain)
    {
        text = null;
        plain = false;
        if (call.ArgumentList.Arguments is not [{ Expression: LambdaExpressionSyntax lambda, NameColon: null, RefKindKeyword.RawKind: 0 }])
        {
            return "ConstructUsing is not given a lambda (a method group or ConstructUsing<TServiceConstructor>()); write the construction by hand";
        }

        ParameterSyntax? parameterSyntax = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => simple.Parameter,
            ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [{ } only] } => only,
            _ => null,
        };
        if (parameterSyntax is null)
        {
            return "ConstructUsing takes a ResolutionContext (ctx.Mapper, ctx.Items), which only exists while AutoMapper maps; write the construction by hand";
        }

        if (lambda.Body is not ExpressionSyntax body || lambda.AsyncKeyword != default || model.GetDeclaredSymbol(parameterSyntax) is not IParameterSymbol parameter)
        {
            return "ConstructUsing has a lambda without an expression body; write the construction by hand";
        }

        if (destination is not INamedTypeSymbol { IsAbstract: false } || destination.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            return $"{destination.Name} is abstract or no class or struct; ConstructUsing may return a derived type, write the construction by hand";
        }

        List<SimpleNameSyntax> parameterUses = [];
        foreach (SyntaxNode node in body.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case ThisExpressionSyntax or BaseExpressionSyntax:
                    return "ConstructUsing uses this or base of the profile";
                case SimpleNameSyntax name when MapAccessibility.IsReference(name):
                    ISymbol? symbol = model.GetSymbolInfo(name).Symbol;
                    if (SymbolEqualityComparer.Default.Equals(symbol, parameter))
                    {
                        parameterUses.Add(name);
                    }
                    else if (name.Identifier.ValueText is "source" or "destination" && symbol is not INamedTypeSymbol)
                    {
                        return $"ConstructUsing already uses the name {name.Identifier.ValueText} for something else";
                    }
                    else if (!MapAccessibility.IsAvailableOutsideProfile(symbol, body, profileType))
                    {
                        return $"ConstructUsing uses {name.Identifier.ValueText}, which is not available outside the profile (a constructor parameter, field or private member of it)";
                    }

                    break;
            }
        }

        if (model.GetTypeInfo(body).Type is not { TypeKind: not TypeKind.Error })
        {
            return "ConstructUsing returns an expression without a type of its own (a null literal, for one)";
        }

        ExpressionSyntax replaced = parameter.Name == "source"
            ? body
            : body.ReplaceNodes(parameterUses, (original, _) => SyntaxFactory.IdentifierName("source").WithTriviaFrom(original));
        text = replaced.WithoutTrivia().ToString();
        plain = body is BaseObjectCreationExpressionSyntax { Initializer: null };
        return null;
    }

    // Why a ForPath(d => d.A.B, o => o.MapFrom(...)) group cannot be written as A = new() { B = ... }, or null with that value in created.
    // Measured (TAAK-255): on a new object AutoMapper creates A with its parameterless constructor (initializers run) even when every
    // value is null, and a ForPath MapFrom is null-safe like a ForMember one.
    internal static string? PathProblem(ISymbol target, List<(string Member, SimpleLambdaExpressionSyntax MapFrom)> path, ITypeSymbol source,
        Dictionary<string, SimpleLambdaExpressionSyntax?> options, MemberOptions extra, SemanticModel model, INamedTypeSymbol? profileType, MemberValues values, out string created)
    {
        created = "";
        ITypeSymbol type = target is IPropertySymbol property ? property.Type : ((IFieldSymbol)target).Type;
        if (!(target is IFieldSymbol { IsReadOnly: false } || target is IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public }))
        {
            return $"ForPath writes into {target.Name}, which has no public setter (AutoMapper fills the object it already holds)";
        }

        if (options.ContainsKey(target.Name) || extra.Conditions.ContainsKey(target.Name) || MayFlatten(target.Name, source))
        {
            return $"ForPath writes into {target.Name}, which AutoMapper also maps itself (from a member of {source.Name} or a ForMember option)";
        }

        if (type is not INamedTypeSymbol { IsAbstract: false, TypeKind: TypeKind.Class } named
            || !named.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
        {
            return $"ForPath writes into {target.Name}, whose type {type.Name} is not a class with a public parameterless constructor";
        }

        List<string> assignments = [];
        foreach ((string member, SimpleLambdaExpressionSyntax mapFrom) in path)
        {
            if (CreateMapPattern.AllMembers(named).FirstOrDefault(m => m.Name == member) is not ({ } inner and (IFieldSymbol { IsReadOnly: false } or IPropertySymbol { SetMethod.DeclaredAccessibility: Accessibility.Public })))
            {
                return $"ForPath to {target.Name}.{member}: {type.Name} has no public settable member {member}";
            }

            ITypeSymbol innerType = inner is IPropertySymbol innerProperty ? innerProperty.Type : ((IFieldSymbol)inner).Type;
            if (MapFromProblem(target.Name + "." + member, mapFrom, model, profileType, innerType, named, values, nullSafe: false, out string value, out _, out _, out _, out _) is string problem)
            {
                return problem;
            }

            assignments.Add($"{member} = {value}");
        }

        created = $"new() {{ {string.Join(", ", assignments)} }}";
        return null;
    }

    // Why a Condition or PreCondition cannot become X = cond ? value : default, or null with the members rewritten that way. Measured (TAAK-255):
    // when the condition is false AutoMapper leaves the member at the value the new object has (its initializer or default); Condition
    // runs after the value is resolved (a MapFrom still runs), PreCondition before; a condition that dereferences null throws.
    internal static string? ConditionProblem(ITypeSymbol destination, MemberOptions extra, SemanticModel model, INamedTypeSymbol? profileType, MemberValues values,
        List<(string Name, string Value)> members)
    {
        ITypeSymbol boolean = model.Compilation.GetSpecialType(SpecialType.System_Boolean);
        foreach ((string name, SimpleLambdaExpressionSyntax? lambda) in extra.Conditions)
        {
            int index = members.FindIndex(m => m.Name == name);
            if (index < 0 || lambda is null)
            {
                return $"the Condition for {name} is on a member the map does not write (Ignore or no public setter)";
            }

            ISymbol member = CreateMapPattern.AllMembers(destination).First(m => m.Name == name);
            if (!KeepsDefault(member, destination))
            {
                return $"{destination.Name}.{name} has an initializer or {destination.Name} a constructor of its own: when the Condition is false AutoMapper keeps that value, which an object initializer cannot";
            }

            if (MapFromProblem("Condition of " + name, lambda, model, profileType, boolean, destination, values, nullSafe: false, out string condition, out _, out _, out _, out _) is string problem)
            {
                return problem;
            }

            ITypeSymbol type = member is IPropertySymbol property ? property.Type : ((IFieldSymbol)member).Type;
            string fallback = type.IsValueType || type.NullableAnnotation == NullableAnnotation.Annotated ? "default" : "default!";
            members[index] = (name, $"{Grouped(condition)} ? {Grouped(members[index].Value)} : {fallback}");
        }

        return null;
    }

    // Why a member without a source member of its exact name cannot be read the way AutoMapper reads it, or null with the expression in read.
    internal static string? FlattenedProblem(ITypeSymbol source, ITypeSymbol destination, string name, ITypeSymbol to, MemberValues values,
        out string read, out ITypeSymbol from, out string plain, out ITypeSymbol plainType)
    {
        read = "";
        from = to;
        plain = "";
        plainType = to;
        if (Flattening.Problem(source, destination, name, out IReadOnlyList<ISymbol> path) is string problem)
        {
            return problem;
        }

        if (path.Count == 0)
        {
            return $"{destination.Name}.{name} has no member with the same name in {source.Name}, nor one AutoMapper flattens or reads through a Get method";
        }

        plain = "source" + string.Concat(path.Select(m => "." + m.Name + (m is IMethodSymbol ? "()" : "")));
        plainType = path[^1] switch { IPropertySymbol p => p.Type, IFieldSymbol f => f.Type, _ => ((IMethodSymbol)path[^1]).ReturnType };
        return Flattening.Value(path, to, values.NullableWarnings, out read, out from) is string valueProblem
            ? $"{destination.Name}.{name}: {valueProblem}"
            : null;
    }

    // Whether a new destination holds the default value in member before mapping: no initializer and no constructor declared on the way up.
    internal static bool KeepsDefault(ISymbol member, ITypeSymbol destination)
    {
        if (member.DeclaringSyntaxReferences.Length == 0 || member.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is PropertyDeclarationSyntax { Initializer: not null } or VariableDeclaratorSyntax { Initializer: not null } or ParameterSyntax))
        {
            return false;
        }

        for (ITypeSymbol? current = destination; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            if (current is not INamedTypeSymbol named || named.InstanceConstructors.Any(c => !c.IsImplicitlyDeclared) || current.DeclaringSyntaxReferences.Length == 0)
            {
                return false;
            }
        }

        return true;
    }

    // An expression that may not stand bare as a part of cond ? a : b.
    internal static string Grouped(string expression) =>
        SyntaxFactory.ParseExpression(expression) is ConditionalExpressionSyntax or AssignmentExpressionSyntax or LambdaExpressionSyntax or SwitchExpressionSyntax
            ? $"({expression})"
            : expression;

    // Why the single constructor cannot be called as new(source.X, ...), or null with that call in creation. AutoMapper fills each parameter
    // from the source member of the same name in any case (measured, TAAK-251); a parameter without one gets its default value or makes Map throw.
    internal static string? ConstructorProblem(IMethodSymbol constructor, ITypeSymbol source, ITypeSymbol destination,
        Dictionary<string, SimpleLambdaExpressionSyntax?> options, List<ISymbol> readable, MemberValues values,
        Dictionary<string, SimpleLambdaExpressionSyntax>? ctorParams, SemanticModel model, INamedTypeSymbol? profileType, out string creation, ref string? copied)
    {
        creation = "";
        List<string> arguments = [];
        foreach (IParameterSymbol parameter in constructor.Parameters)
        {
            if (parameter.RefKind != RefKind.None || parameter.IsParams)
            {
                return $"the constructor of {destination.Name} has a ref, out or params parameter {parameter.Name}";
            }

            if (options.Keys.FirstOrDefault(k => string.Equals(k, parameter.Name, StringComparison.OrdinalIgnoreCase)) is { } configured)
            {
                return $"ForMember configures {configured}, which the constructor parameter {parameter.Name} of {destination.Name} fills (AutoMapper may apply it to the parameter)";
            }

            if (ctorParams?.TryGetValue(parameter.Name, out SimpleLambdaExpressionSyntax? ctorLambda) == true)
            {
                // Measured (TAAK-270, AutoMapper 14.0.0): the MapFrom of ForCtorParam is null-safe like a member's MapFrom.
                if (MapFromProblem("ForCtorParam " + parameter.Name, ctorLambda, model, profileType, parameter.Type, destination, values, nullSafe: true, out string fromParameter,
                    out bool parameterCopies, out _, out _, out _) is string ctorProblem)
                {
                    return ctorProblem;
                }

                arguments.Add(fromParameter);
                copied ??= parameterCopies ? parameter.Name : null;
                continue;
            }

            List<ISymbol> matches = [.. readable.Where(m => string.Equals(m.Name, parameter.Name, StringComparison.OrdinalIgnoreCase))];
            if (matches is not [{ } match])
            {
                return matches.Count == 0
                    ? $"the constructor parameter {parameter.Name} of {destination.Name} has no member with the same name in {source.Name} (AutoMapper flattens, uses the default value or throws)"
                    : $"the constructor parameter {parameter.Name} of {destination.Name} matches several members of {source.Name}";
            }

            ITypeSymbol from = match is IPropertySymbol property ? property.Type : ((IFieldSymbol)match).Type;
            if (values.Problem(parameter.Name, "source." + match.Name, from, parameter.Type, destination, out string? argument, out bool copy) is string valueProblem)
            {
                return valueProblem;
            }

            if (argument is null)
            {
                return $"the constructor parameter {parameter.Name} is {parameter.Type.Name} but {match.Name} is {from.Name} in {source.Name} (AutoMapper converts){MemberValues.NoMap(from, parameter.Type)}";
            }

            arguments.Add(argument);
            copied ??= copy ? parameter.Name : null;
        }

        if (ctorParams?.Keys.FirstOrDefault(k => !constructor.Parameters.Any(p => string.Equals(p.Name, k, StringComparison.OrdinalIgnoreCase))) is { } unknown)
        {
            return $"ForCtorParam names {unknown}, which is no parameter of the constructor AutoMapper uses for {destination.Name}";
        }

        creation = $"new({string.Join(", ", arguments)})";
        return null;
    }

    // Why the reverse map B → A of CreateMap<A, B>(forward).ReverseMap() cannot be written as plain assignments, or null with its members.
    // Measured on AutoMapper 14 (TAAK-249): the reverse map has MemberList.None and maps same-named members like any map; a forward Ignore
    // does not carry over; a forward MapFrom(s => s.X) for Y becomes X = source.Y (a longer path is unflattened, s => s maps into A itself);
    // other MapFrom expressions are not reversed; ReverseMap().ForMember options configure the reverse map and win over a reversed MapFrom;
    // a forward member AutoMapper flattened is unflattened.
    internal static string? Reverse(ITypeSymbol source, ITypeSymbol destination, Dictionary<string, SimpleLambdaExpressionSyntax?> forward,
        Dictionary<string, SimpleLambdaExpressionSyntax?> options, MemberOptions extra, SemanticModel model, INamedTypeSymbol? profileType, MemberValues values,
        IReadOnlySet<string> mapInto, out List<(string Name, string Value)> members, out bool needsMapInto, out string creation)
    {
        members = [];
        creation = "new()";
        needsMapInto = mapInto.Contains(MapIntoPattern.Key(destination, source));
        Dictionary<string, string> reversed = new(StringComparer.Ordinal);
        foreach ((string name, SimpleLambdaExpressionSyntax? mapFrom) in forward)
        {
            if (mapFrom?.Body is not ExpressionSyntax body)
            {
                continue;
            }

            ExpressionSyntax bare = Unwrap(body);
            if (bare is CastExpressionSyntax)
            {
                return $"MapFrom for {name} casts; whether AutoMapper reverses it is not known";
            }

            if (MemberPath(bare, model, mapFrom) is not { } path)
            {
                continue;
            }

            if (path is not [{ } member])
            {
                return path.Count == 0
                    ? $"MapFrom for {name} maps the whole {source.Name}; in reverse AutoMapper maps {name} into the {source.Name} itself"
                    : $"MapFrom for {name} is the path {bare}; in reverse AutoMapper unflattens it (creating the objects on the way)";
            }

            if (!reversed.TryAdd(member, name))
            {
                return $"MapFrom for {reversed[member]} and {name} both read {member}; in reverse AutoMapper writes {member} from one of them";
            }
        }

        // A destination member of the forward map that AutoMapper filled by flattening (B.CustomerName from A.Customer.Name) is unflattened
        // in reverse; the forward rewrite only writes members of the same name, so this only concerns members it leaves alone (not Ignore()d ones).
        List<ISymbol> sourceMembers = [.. CreateMapPattern.AllMembers(source).Where(m => m is IPropertySymbol { IsIndexer: false } or IFieldSymbol)];
        foreach (ISymbol member in CreateMapPattern.AllMembers(destination).Where(m => m is IPropertySymbol { IsIndexer: false } or IFieldSymbol))
        {
            if (forward.TryGetValue(member.Name, out SimpleLambdaExpressionSyntax? configured) && configured is null)
            {
                continue;
            }

            // Measured (TAAK-273): only a member the forward map flattens (a path of two or more members) is unflattened; a mere name prefix
            // (ShelfDto.WarehouseName next to ShelfModel.Warehouse without a Name) is not.
            if (!sourceMembers.Any(m => string.Equals(m.Name, member.Name, StringComparison.OrdinalIgnoreCase))
                && sourceMembers.FirstOrDefault(m => member.Name.Length > m.Name.Length && member.Name.StartsWith(m.Name, StringComparison.OrdinalIgnoreCase)) is { } prefix
                && (Flattening.Problem(source, destination, member.Name, out IReadOnlyList<ISymbol> path) is not null || path.Count > 1))
            {
                return $"{destination.Name}.{member.Name} may be unflattened into {source.Name}.{prefix.Name} by AutoMapper";
            }
        }

        if (needsMapInto && (!extra.IsEmpty || extra.HasForPath))
        {
            return "a Condition or ForPath is not migrated together with Map(source, destination) (MapInto)";
        }

        // Measured (TAAK-264): a Condition acts on a reversed value like on any other; a ForPath next to a reversed MapFrom of the same member was not measured.
        if (extra.Paths.Keys.FirstOrDefault(reversed.ContainsKey) is { } pathTarget)
        {
            return $"ForPath writes into {pathTarget}, which the reversed MapFrom of the forward map also fills";
        }

        if (Problem(destination, source, options, model, profileType, values, out members, out string? copied, out creation, reversed, extra: extra) is string problem)
        {
            return problem;
        }

        // Not measured how a reversed MapFrom reaches a constructor parameter, nor can MapInto set one (TAAK-251).
        if (creation != "new()" && (reversed.Count > 0 || needsMapInto))
        {
            return $"{source.Name} is created through its constructor, which a reversed MapFrom or MapInto cannot be applied to";
        }

        // Measured (TAAK-245, again for the reverse map): Map(source, destination) maps into the nested object or list the destination holds.
        return needsMapInto && copied is not null
            ? $"Map(source, destination) maps {copied} into the object or collection the destination already holds, MapInto would replace it"
            : null;
    }

    // The members of `p.X.Y` (outermost last) when expression is a path of properties and fields from the lambda parameter p, [] for `p` itself, else null.
    internal static List<string>? MemberPath(ExpressionSyntax expression, SemanticModel model, SimpleLambdaExpressionSyntax lambda)
    {
        List<string> path = [];
        while (Unwrap(expression) is MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } access
            && model.GetSymbolInfo(access).Symbol is IPropertySymbol or IFieldSymbol { IsStatic: false })
        {
            path.Insert(0, access.Name.Identifier.ValueText);
            expression = access.Expression;
        }

        return Unwrap(expression) is IdentifierNameSyntax name && SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name).Symbol, model.GetDeclaredSymbol(lambda.Parameter))
            ? path
            : null;
    }

    // Parentheses and the null-forgiving ! are not in the expression tree AutoMapper sees.
    internal static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => Unwrap(parenthesized.Expression),
        PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppressed => Unwrap(suppressed.Operand),
        _ => expression,
    };

    // Whether AutoMapper fills a get-only member of this type with the source items (measured on List and IReadOnlyList, TAAK-261):
    // any enumerable except an array or a string.
    internal static bool IsFilledCollection(ITypeSymbol type) =>
        type is not IArrayTypeSymbol && type.SpecialType != SpecialType.System_String
        && (type.SpecialType == SpecialType.System_Collections_IEnumerable || type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable));

    // Whether AutoMapper may fill a destination member that has no source member of its exact name: a member or method of that name in
    // another case or with a Get prefix, or one whose name starts the member's name (flattening: Customer.Name → CustomerName).
    internal static bool MayFlatten(string name, ITypeSymbol source) =>
        CreateMapPattern.AllMembers(source).Any(m => (m is IPropertySymbol or IFieldSymbol || m is IMethodSymbol { MethodKind: MethodKind.Ordinary, Parameters: [], ReturnsVoid: false })
            && (string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase) || string.Equals(m.Name, "Get" + name, StringComparison.OrdinalIgnoreCase)
                || (name.Length > m.Name.Length && name.StartsWith(m.Name, StringComparison.OrdinalIgnoreCase))));

    // Why `s => expr` cannot move into the extension method as `X = expr`, or null with the text of expr (s becomes source).
    // AutoMapper runs a MapFrom expression null-safe (a NullReferenceException gives the default value) and converts types
    // where needed, a plain assignment does neither: so only expressions that cannot throw and need no conversion are moved.
    internal static string? MapFromProblem(string member, SimpleLambdaExpressionSyntax lambda, SemanticModel model, INamedTypeSymbol? profileType,
        ITypeSymbol targetType, ITypeSymbol destination, MemberValues values, bool nullSafe, out string value, out bool copies,
        out string read, out ITypeSymbol? readType, out string? readProblem)
    {
        readProblem = null;
        value = "";
        copies = false;
        read = "";
        readType = null;
        if (lambda.Body is not ExpressionSyntax body || model.GetDeclaredSymbol(lambda.Parameter) is not IParameterSymbol parameter || lambda.AsyncKeyword != default)
        {
            return $"MapFrom for {member} is not a lambda with an expression body";
        }

        string parameterName = parameter.Name;
        string? nullProblem = null;
        List<SimpleNameSyntax> parameterUses = [];
        foreach (SyntaxNode node in body.DescendantNodesAndSelf())
        {
            ExpressionSyntax? receiver = null;
            switch (node)
            {
                case ThisExpressionSyntax or BaseExpressionSyntax:
                    return $"MapFrom for {member} uses this or base of the profile";
                case SimpleNameSyntax name when MapAccessibility.IsReference(name):
                    ISymbol? symbol = model.GetSymbolInfo(name).Symbol;
                    if (SymbolEqualityComparer.Default.Equals(symbol, parameter))
                    {
                        parameterUses.Add(name);
                    }
                    else if (name.Identifier.ValueText == "source" && parameterName != "source" && symbol is not INamedTypeSymbol)
                    {
                        return $"MapFrom for {member} already uses the name source for something else";
                    }
                    else if (!(symbol is not null && values.Captured.Contains(symbol)) && !MapAccessibility.IsAvailableOutsideProfile(symbol, body, profileType))
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

            if (receiver is not null && nullProblem is null && MayBeNull(receiver, model, parameter))
            {
                nullProblem = $"MapFrom for {member} dereferences {receiver.ToString().Trim()}, which may be null (AutoMapper returns the default value, a plain assignment throws)";
            }
        }

        // A plain chain of members is null-safe with ?. (and ?? default for a value type): the same default AutoMapper returns (TAAK-263).
        string? chain = null;
        if (nullProblem is not null)
        {
            chain = nullSafe ? NullSafeChain(body, model, parameter) : null;
            if (chain is null)
            {
                return nullProblem;
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
        bool nullable = from.IsValueType && from.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        if (chain is not null)
        {
            readProblem = nullProblem!.Replace("a plain assignment throws", "an expression tree throws", StringComparison.Ordinal);
            value = from.IsValueType && !nullable ? chain + " ?? default"
                : values.NullableWarnings && targetType.IsReferenceType && targetType.NullableAnnotation == NullableAnnotation.NotAnnotated ? chain + "!" : chain;
        }

        string plainValue = value;
        if (values.Problem("MapFrom for " + member, value, from, targetType, destination, out string? mapped, out copies) is string valueProblem)
        {
            return valueProblem;
        }

        if (chain is not null && mapped is not null && mapped != plainValue)
        {
            return nullProblem;
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

    // `source.A?.B.C`: body (a plain chain of properties and fields from the lambda parameter) over the name source, with ?. after every
    // receiver that may be null, or null when body is anything else.
    internal static string? NullSafeChain(ExpressionSyntax body, SemanticModel model, IParameterSymbol parameter)
    {
        List<MemberAccessExpressionSyntax> links = [];
        ExpressionSyntax current = Unwrap(body);
        while (current is MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } access
            && model.GetSymbolInfo(access).Symbol is IPropertySymbol { IsStatic: false } or IFieldSymbol { IsStatic: false })
        {
            links.Insert(0, access);
            current = Unwrap(access.Expression);
        }

        if (links.Count == 0 || current is not IdentifierNameSyntax name || !SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name).Symbol, parameter))
        {
            return null;
        }

        StringBuilder chain = new("source");
        foreach (MemberAccessExpressionSyntax link in links)
        {
            chain.Append(MayBeNull(link.Expression, model, parameter) ? "?." : ".").Append(link.Name.Identifier.ValueText);
        }

        return chain.ToString();
    }

    // Whether `receiver.Something` can throw: a reference type or Nullable<T> that is not the lambda parameter and not known to be non-null.
    internal static bool MayBeNull(ExpressionSyntax receiver, SemanticModel model, IParameterSymbol parameter)
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
}
