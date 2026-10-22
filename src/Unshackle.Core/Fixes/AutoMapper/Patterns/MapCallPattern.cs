using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Models;

namespace Unshackle.Core.Fixes.AutoMapper.Patterns;

/// <summary>
/// mapper.Map&lt;B&gt;(a) and mapper.Map&lt;A, B&gt;(a) → a.ToB(): the extension method that already exists, or the one
/// <see cref="CreateMapPattern"/> (or a ForMember pattern) writes for a CreateMap&lt;A, B&gt; in a profile of the solution.
/// The IMapper the call came from stays: removing a constructor parameter changes every caller.
/// A collection (Map&lt;List&lt;B&gt;&gt;(items)) becomes items?.Select(x =&gt; x?.ToB()).ToList() ?? [] like a nested collection member
/// (<see cref="MemberValues"/>); a source that may be null becomes a?.ToB() where the call's target allows null (TAAK-259).
/// </summary>
public class MapCallPattern : ISolutionPattern, ISolutionFinder
{
    private const string AutoMapperAssembly = "AutoMapper";

    public string Id => "automapper.map-call";

    public string Description =>
        "mapper.Map<B>(a) → a.ToB(), calling the extension method of CreateMap<A, B>; Map<List<B>>(items) → items.Select(x => x.ToB()).ToList(); unknown maps are Manual";

    // Also every Map or ProjectTo of AutoMapper used as a method group (items.Select(mapper.Map<B>), TAAK-267): left alone it would still
    // run after its CreateMap is gone, so it is either rewritten to a lambda or Manual (which blocks the whole fix).
    public IEnumerable<SyntaxNode> Find(SyntaxNode root, SemanticModel model) =>
        root.DescendantNodes().Where(n => n is InvocationExpressionSyntax i ? IsMapCall(i, model) : IsMethodGroup(n, model));

    // TAAK-311: the Map call of a stub that MockSetUpPattern removes is not a finding of its own.
    public async Task<IReadOnlyList<SyntaxNode>> FindAsync(SyntaxNode root, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        List<SyntaxNode> found = [.. Find(root, model)];
        if (!found.Any(n => MapOwners.IsMockSetUp(n, model)))
        {
            return found;
        }

        MapOwners owners = await MapOwners.ForAsync(solution, model, cancellationToken);
        HashSet<SyntaxNode> removed = [.. MockedMappers.Find(root, model).Where(m => owners.IsDischarged(m.Key)).SelectMany(m => m.Stubs.Select(s => (SyntaxNode)s.Call))];
        return [.. found.Where(n => !removed.Contains(n))];
    }

    // Without the solution only an extension method the code already has counts.
    public PatternResult Rewrite(SyntaxNode node, SemanticModel model)
    {
        if (node is not InvocationExpressionSyntax)
        {
            if (CheckGroup((ExpressionSyntax)node, model, out ITypeSymbol? groupSource, out ITypeSymbol? groupDestination) is string groupProblem)
            {
                return PatternResult.Manual(groupProblem);
            }

            ExpressionSyntax parameter = SyntaxFactory.IdentifierName(ParameterName(node, model));
            return Lambda(parameter, Existing(node, model, groupSource!, groupDestination!, parameter, MapCallKind.Plain) ?? PatternResult.Manual(NoMap(groupSource!, groupDestination!)));
        }

        if (Check((InvocationExpressionSyntax)node, model, out ExpressionSyntax? source, out ITypeSymbol? sourceType, out ITypeSymbol? destination, out MapCallKind kind)
            is string problem)
        {
            return PatternResult.Manual(problem);
        }

        return kind == MapCallKind.Collection
            ? Collection((InvocationExpressionSyntax)node, model, NestedMaps.From(model), source!, sourceType!, destination!)
            : Existing(node, model, sourceType!, destination!, source!, kind) ?? PatternResult.Manual(NoMap(sourceType!, destination!));
    }

    public async Task<PatternResult> RewriteAsync(SyntaxNode node, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        if (await MapOwners.KeptCallAsync(node, model, solution, cancellationToken) is { } kept)
        {
            return kept;
        }

        if (node is not InvocationExpressionSyntax)
        {
            if (CheckGroup((ExpressionSyntax)node, model, out ITypeSymbol? groupSource, out ITypeSymbol? groupDestination) is string groupProblem)
            {
                return PatternResult.Manual(groupProblem);
            }

            ExpressionSyntax parameter = SyntaxFactory.IdentifierName(ParameterName(node, model));
            return Lambda(parameter, await SingleAsync(node, model, solution, parameter, groupSource!, groupDestination!, MapCallKind.Plain, cancellationToken));
        }

        if (Check((InvocationExpressionSyntax)node, model, out ExpressionSyntax? source, out ITypeSymbol? sourceType, out ITypeSymbol? destination, out MapCallKind kind)
            is string problem)
        {
            return PatternResult.Manual(problem);
        }

        if (kind == MapCallKind.Collection)
        {
            return Collection((InvocationExpressionSyntax)node, model, await NestedMaps.FromAsync(model, solution, cancellationToken), source!, sourceType!, destination!);
        }

        return await SingleAsync(node, model, solution, source!, sourceType!, destination!, kind, cancellationToken);
    }

    // source.ToB() through the method that exists or the one a profile of the solution gets; Manual when there is none (or several).
    private static async Task<PatternResult> SingleAsync(SyntaxNode node, SemanticModel model, Solution solution, ExpressionSyntax source, ITypeSymbol sourceType,
        ITypeSymbol destination, MapCallKind kind, CancellationToken cancellationToken)
    {
        if (Existing(node, model, sourceType!, destination!, source!, kind) is { } existing)
        {
            return existing;
        }

        List<(string Method, string? Namespace, bool IsInternal, ProjectId Project)> generated =
            await GeneratedAsync(solution, model, sourceType!, destination!, cancellationToken);
        if (generated.Count == 0)
        {
            return PatternResult.Manual(await MapBlockers.ReasonAsync(solution, model, sourceType!, destination!, cancellationToken) ?? NoMapAnywhere(sourceType!, destination!));
        }

        if (generated.Count > 1)
        {
            return PatternResult.Manual(MapBlockers.Several(sourceType!, destination!));
        }

        (string method, string? ns, bool isInternal, ProjectId project) = generated[0];
        ProjectId? callProject = solution.GetDocument(model.SyntaxTree)?.Project.Id;
        if (isInternal && callProject != project)
        {
            return PatternResult.Manual($"The mapping method {method} becomes internal in another project; call it by hand or make the types public.");
        }

        if (model.LookupSymbols(node.SpanStart, sourceType, method).Any())
        {
            return PatternResult.Manual($"{sourceType!.Name} already has a member {method}; the extension method would not be called.");
        }

        string? callNamespace = model.GetEnclosingSymbol(node.SpanStart, cancellationToken)?.ContainingNamespace is { IsGlobalNamespace: false } current ? current.ToDisplayString() : null;
        bool imported = ns is null || (callNamespace is not null && (callNamespace == ns || callNamespace.StartsWith(ns + ".", StringComparison.Ordinal)));
        return Fixed(node, source!, method, imported ? [] : [ns!], kind);
    }

    // Map<B>(source) or Map<A, B>(source) of AutoMapper; Map(source, destination), Map with options and ProjectTo stay with CreateMapPattern.
    internal static bool IsMapCall(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.ArgumentList.Arguments.Count == 1
        && model.GetSymbolInfo(invocation) is var info
        && (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) is IMethodSymbol { Name: "Map", TypeArguments.Length: 1 or 2, Parameters.Length: 1 } method
        && method.ContainingAssembly?.Name == AutoMapperAssembly;

    // A reference to Map or ProjectTo of AutoMapper that is not called: mapper.Map<B> (or Map<B> inside a mapper) passed as a delegate.
    // nameof(mapper.Map) runs nothing and is left alone.
    internal static bool IsMethodGroup(SyntaxNode node, SemanticModel model)
    {
        if (node is not (MemberAccessExpressionSyntax or SimpleNameSyntax) || node.Parent is MemberAccessExpressionSyntax { Name: var name } && name == node
            || node.Parent is MemberBindingExpressionSyntax || node.Parent is InvocationExpressionSyntax { Expression: var called } && called == node
            || node.Ancestors().OfType<InvocationExpressionSyntax>().Any(i => i.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }))
        {
            return false;
        }

        IMethodSymbol? method = model.GetOperation(node) is IMethodReferenceOperation reference ? reference.Method
            : model.GetSymbolInfo(node) is var info ? (info.Symbol ?? info.CandidateSymbols.FirstOrDefault()) as IMethodSymbol : null;
        return method is { Name: "Map" or "ProjectTo" } && method.ContainingAssembly?.Name == AutoMapperAssembly;
    }

    // Why the method group cannot become x => x.ToB(), or null with the types of the map: only Map<B> or Map<A, B> with one parameter,
    // converted to a delegate that takes the source.
    private static string? CheckGroup(ExpressionSyntax group, SemanticModel model, out ITypeSymbol? sourceType, out ITypeSymbol? destination)
    {
        sourceType = null;
        destination = null;
        if (model.GetOperation(group) is not IMethodReferenceOperation { Method: { Name: "Map", TypeArguments.Length: 1 or 2, Parameters.Length: 1 } method } reference
            || reference.Parent is not IDelegateCreationOperation { Type: INamedTypeSymbol { DelegateInvokeMethod: { Parameters: [{ } parameter] } } })
        {
            return "Map (or ProjectTo) is passed as a method group in a form this fix does not rewrite (only Map<B> with one source does); write a lambda by hand, e.g. x => x.To…().";
        }

        if (group is MemberAccessExpressionSyntax { Expression: var receiver } && !HasNoSideEffects(receiver))
        {
            return "Map is passed as a method group of something other than a plain field, property or variable; migrate it by hand.";
        }

        if (group.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: true } } })
        {
            return "Map is assigned as a method group to a var; a lambda has no type of its own there; migrate it by hand.";
        }

        destination = method.TypeArguments[^1];
        sourceType = method.TypeArguments.Length == 2 ? method.TypeArguments[0] : parameter.Type;
        if (sourceType is { TypeKind: TypeKind.Error or TypeKind.Dynamic or TypeKind.TypeParameter } || sourceType.SpecialType == SpecialType.System_Object)
        {
            return "The source of the Map method group has no type of its own (object, dynamic or a type parameter); AutoMapper picks the map at run time.";
        }

        if (IsCollection(sourceType) || IsCollection(destination))
        {
            return $"Map to {destination.ToDisplayString()} is passed as a method group over a collection; write the Select by hand.";
        }

        if (parameter.Type.NullableAnnotation == NullableAnnotation.Annotated || destination.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return $"The source of the Map method group may be null: AutoMapper returns null, {sourceType.Name}.To{destination.Name}() throws; check for null and migrate it by hand.";
        }

        return null;
    }

    // The lambda parameter: source, or a name that is not taken where the method group is.
    private static string ParameterName(SyntaxNode node, SemanticModel model)
    {
        string name = "source";
        for (int i = 2; model.LookupSymbols(node.SpanStart, name: name).Any(); i++)
        {
            name = "source" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return name;
    }

    // x.ToB() of the rewrite inside x => x.ToB().
    private static PatternResult Lambda(ExpressionSyntax parameter, PatternResult result) => result is FixedResult { NewNodes: [ExpressionSyntax body] } fixedResult
        ? PatternResult.Fixed(SyntaxFactory.ParseExpression($"{parameter} => {body.NormalizeWhitespace()}"), fixedResult.RequiredNamespaces)
        : result;

    // Why the call cannot become source.ToB(),or null with the source expression and the types of the map.
    private static string? Check(InvocationExpressionSyntax invocation, SemanticModel model, out ExpressionSyntax? source, out ITypeSymbol? sourceType,
        out ITypeSymbol? destination, out MapCallKind kind)
    {
        kind = MapCallKind.Plain;
        source = invocation.ArgumentList.Arguments[0].Expression;
        sourceType = null;
        destination = null;
        if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method)
        {
            return "Map does not bind to one AutoMapper method; migrate it by hand.";
        }

        destination = method.TypeArguments[^1];
        sourceType = method.TypeArguments.Length == 2 ? method.TypeArguments[0] : model.GetTypeInfo(source).Type;
        if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: var receiver } || !HasNoSideEffects(receiver))
        {
            return "Map is not called as mapper.Map on a plain field, property or variable; migrate it by hand.";
        }

        if (sourceType is null or { TypeKind: TypeKind.Error or TypeKind.Dynamic } || sourceType.SpecialType == SpecialType.System_Object)
        {
            // Map<B>(object) maps the run-time type; the static type does not say which CreateMap runs.
            return "The source of Map has no type of its own (object or dynamic); AutoMapper picks the map at run time.";
        }

        if (IsCollection(sourceType) || IsCollection(destination))
        {
            kind = MapCallKind.Collection;
            return null;
        }

        if (model.GetTypeInfo(source).Nullability.FlowState == NullableFlowState.MaybeNull)
        {
            // Measured (TAAK-259, TAAK-273): AutoMapper 14 returns null for a null source; a?.ToB() does the same, with ! where the target
            // does not allow null (the null went there before as well). Map<Struct>(null) is not measured.
            if (destination.IsValueType)
            {
                return $"The source of Map may be null: AutoMapper returns null, {sourceType.Name}.To{destination.Name}() throws, and the result goes where null is not allowed; check for null and migrate it by hand.";
            }

            kind = TargetAllowsNull(invocation, model, destination) ? MapCallKind.Conditional : MapCallKind.Suppressed;
        }

        return null;
    }

    // Map<B?>(a), or a call returned from (or assigned to) something declared nullable: a?.ToB() adds no nullable warning there.
    // A var local is left out: it would become B? and warn where it is used.
    private static bool TargetAllowsNull(InvocationExpressionSyntax invocation, SemanticModel model, ITypeSymbol destination)
    {
        if (destination.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        SyntaxNode expression = invocation;
        while (expression.Parent is ParenthesizedExpressionSyntax)
        {
            expression = expression.Parent;
        }

        ITypeSymbol? target = expression.Parent switch
        {
            ReturnStatementSyntax or ArrowExpressionClauseSyntax => ReturnType(model, expression),
            EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: false } } declarator } =>
                DeclaredType(model.GetDeclaredSymbol(declarator)),
            EqualsValueClauseSyntax { Parent: PropertyDeclarationSyntax property } => DeclaredType(model.GetDeclaredSymbol(property)),
            AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment when assignment.Right == expression =>
                DeclaredType(model.GetSymbolInfo(assignment.Left).Symbol),
            _ => null,
        };
        return target is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.Annotated };
    }

    private static ITypeSymbol? DeclaredType(ISymbol? symbol) => symbol switch
    {
        ILocalSymbol local => local.Type,
        IFieldSymbol field => field.Type,
        IPropertySymbol property => property.Type,
        IParameterSymbol parameter => parameter.Type,
        _ => null,
    };

    // What a return (or expression body) at the expression returns: the declared type, Task<T> and ValueTask<T> of an async method unwrapped.
    private static ITypeSymbol? ReturnType(SemanticModel model, SyntaxNode expression)
    {
        if (model.GetEnclosingSymbol(expression.SpanStart) is not IMethodSymbol method || method.MethodKind == MethodKind.AnonymousFunction)
        {
            return null;
        }

        if (method.AssociatedSymbol is IPropertySymbol property)
        {
            return property.Type;
        }

        ITypeSymbol type = method.ReturnType;
        return !method.IsAsync ? type
            : type is INamedTypeSymbol { Name: "Task" or "ValueTask", TypeArguments: [{ } result] } ? result : null;
    }

    // A Map of a collection: each element through the map of its type pair into a new List or array, as AutoMapper 14 does (measured in
    // TAAK-259: a null source gives an empty collection, a null element a null one; IEnumerable, IList and the like get a List).
    private static PatternResult Collection(InvocationExpressionSyntax invocation, SemanticModel model, NestedMaps maps, ExpressionSyntax source, ITypeSymbol sourceType,
        ITypeSymbol destination)
    {
        HashSet<string> namespaces = [];
        var values = new MemberValues(maps, model, invocation.SpanStart, namespaces);
        string value = source.WithoutTrivia().ToString();
        if (values.Problem("The collection", value, sourceType, destination, model.Compilation.GetSpecialType(SpecialType.System_Void), out string? copy, out _)
            is string problem)
        {
            return PatternResult.Manual($"Map to {destination.ToDisplayString()}: {problem}.");
        }

        if (copy is null || copy == value)
        {
            return PatternResult.Manual(
                $"Map to {destination.ToDisplayString()} maps a collection this fix does not copy (from {sourceType.ToDisplayString()}; AutoMapper maps each element into a new one); write it by hand, e.g. with Select(x => x.To…()).");
        }

        // A source that is not null needs no ?. and no empty fallback.
        string receiver = MemberValues.Receiver(value);
        int fallback = copy.LastIndexOf(" ?? ", StringComparison.Ordinal);
        if (model.GetTypeInfo(source).Nullability.FlowState == NullableFlowState.NotNull && copy.StartsWith(receiver + "?.", StringComparison.Ordinal) && fallback > 0)
        {
            copy = receiver + copy[(receiver.Length + 1)..fallback];
        }

        // Elements declared non-null need no ?. either (and then no ! for the null AutoMapper would write).
        if (model.GetNullableContext(invocation.SpanStart).WarningsEnabled() && MemberValues.TryElement(sourceType, out ITypeSymbol? element, out _)
            && element is { IsReferenceType: true, NullableAnnotation: NullableAnnotation.NotAnnotated })
        {
            copy = copy.Replace("(x => x?.", "(x => x.", StringComparison.Ordinal).Replace("()!)", "())", StringComparison.Ordinal);
        }

        ExpressionSyntax rewritten = SyntaxFactory.ParseExpression(copy);
        if (rewritten is BinaryExpressionSyntax && invocation.Parent is not (ArgumentSyntax or EqualsValueClauseSyntax or ArrowExpressionClauseSyntax or ReturnStatementSyntax
            or AssignmentExpressionSyntax or ExpressionStatementSyntax))
        {
            rewritten = SyntaxFactory.ParenthesizedExpression(rewritten);
        }

        return PatternResult.Fixed(rewritten, [.. namespaces.Order(StringComparer.Ordinal)]);
    }

    // An extension method To<B>() on A returning B: one the calling code already sees, or one in this project's source (also the class
    // the profile patterns just wrote, when the profile is in the same file) with a using for its namespace.
    private static PatternResult? Existing(SyntaxNode node, SemanticModel model, ITypeSymbol sourceType, ITypeSymbol destination, ExpressionSyntax source,
        MapCallKind kind)
    {
        string name = MapMethodWriter.MethodName(destination.Name);
        List<IMethodSymbol> visible = [.. model.LookupSymbols(node.SpanStart, sourceType, name, includeReducedExtensionMethods: true)
            .OfType<IMethodSymbol>().Where(m => m.Parameters.All(p => p.IsOptional) && SymbolEqualityComparer.Default.Equals(m.ReturnType, destination))];
        if (visible is [{ ReducedFrom: not null }])
        {
            return Fixed(node, source, name, [], kind);
        }

        if (visible.Count > 0 || model.LookupSymbols(node.SpanStart, sourceType, name).Any())
        {
            return null;
        }

        INamedTypeSymbol? within = model.GetEnclosingSymbol(node.SpanStart)?.ContainingType;
        List<IMethodSymbol> declared = [.. model.Compilation.GetSymbolsWithName(name, SymbolFilter.Member).OfType<IMethodSymbol>()
            .Where(m => m is { IsExtensionMethod: true, Parameters: [{ } self, .. { } rest] } && rest.All(p => p.IsOptional) && SymbolEqualityComparer.Default.Equals(self.Type, sourceType)
                && SymbolEqualityComparer.Default.Equals(m.ReturnType, destination)
                && (within is null ? m.DeclaredAccessibility != Accessibility.Private : model.Compilation.IsSymbolAccessibleWithin(m, within)))];
        return declared is [{ ContainingNamespace: { IsGlobalNamespace: false } ns }]
            ? Fixed(node, source, name, [ns.ToDisplayString()], kind)
            : declared is [_] ? Fixed(node, source, name, [], kind) : null;
    }

    // Every CreateMap<A, B> in a profile the profile patterns rewrite, in the calling project or one it depends on: the generated method,
    // the namespace of its class (null: global) and whether it becomes internal.
    internal static async Task<List<(string Method, string? Namespace, bool IsInternal, ProjectId Project)>> GeneratedAsync(Solution solution, SemanticModel model,
        ITypeSymbol sourceType, ITypeSymbol destination, CancellationToken cancellationToken)
    {
        string sourceName = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string destinationName = destination.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        IEnumerable<Project> projects = solution.GetDocument(model.SyntaxTree)?.Project is { } callProject
            ? solution.GetProjectDependencyGraph().GetProjectsThatThisProjectTransitivelyDependsOn(callProject.Id).Append(callProject.Id).Select(solution.GetProject).OfType<Project>()
            : solution.Projects;
        List<(string Method, string? Namespace, bool IsInternal, ProjectId Project)> found = [];
        MapOwners owners = await MapOwners.ForAsync(solution, model, cancellationToken);
        foreach (Project project in projects.Where(p => p.Language == LanguageNames.CSharp))
        {
            foreach (Document document in project.Documents)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!(await document.GetTextAsync(cancellationToken)).ToString().Contains("CreateMap", StringComparison.Ordinal) || await document.GetSyntaxRootAsync(cancellationToken) is not { } root
                    || await document.GetSemanticModelAsync(cancellationToken) is not { } profileModel)
                {
                    continue;
                }

                foreach (ClassDeclarationSyntax profile in root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => ProfileSyntax.IsProfileWithMaps(c, profileModel)))
                {
                    // With the solution: a member may call the map of a profile in a project this one depends on.
                    if (await CreateMapPattern.RewriteMethodsAsync(profile, profileModel, solution, cancellationToken) is not FixedResult { NewNodes: [_, ClassDeclarationSyntax extensions] })
                    {
                        continue;
                    }

                    // One generated method per CreateMap statement (or expression body), in the same order, followed by the reverse one for a ReverseMap.
                    List<MethodDeclarationSyntax> methods = [.. extensions.Members.OfType<MethodDeclarationSyntax>()];
                    IReadOnlyList<SyntaxNode> statements = ProfileSyntax.MapStatements(profile, profileModel);
                    List<(ITypeSymbol From, ITypeSymbol To)> pairs = [];
                    foreach (SyntaxNode statement in statements)
                    {
                        InvocationExpressionSyntax? createMap = statement.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                            .LastOrDefault(c => c.Expression is GenericNameSyntax { Identifier.ValueText: "CreateMap" } or MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "CreateMap" } });
                        // A map that stays on AutoMapper keeps its statement and gets no method (TAAK-272).
                        if (createMap is not null && profileModel.GetSymbolInfo(createMap, cancellationToken).Symbol is IMethodSymbol { TypeArguments: [{ } from, { } to] }
                            && owners.Reason(from, to) is null)
                        {
                            pairs.Add((from, to));
                            if (ProfileSyntax.HasReverseMap(createMap))
                            {
                                pairs.Add((to, from));
                            }
                        }
                    }

                    for (int i = 0; i < pairs.Count && i < methods.Count; i++)
                    {
                        if (pairs[i].From.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == sourceName
                            && pairs[i].To.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == destinationName)
                        {
                            INamespaceSymbol? ns = profileModel.GetDeclaredSymbol(profile, cancellationToken)?.ContainingNamespace;
                            found.Add((methods[i].Identifier.ValueText, ns is null or { IsGlobalNamespace: true } ? null : ns.ToDisplayString(),
                                extensions.Modifiers.Any(SyntaxKind.InternalKeyword), project.Id));
                        }
                    }
                }
            }
        }

        return found;
    }

    // Conditional: source?.Method() (Suppressed: with !), in parentheses where a member access after it would join the ?. chain (a ?. after it may join).
    private static PatternResult Fixed(SyntaxNode node, ExpressionSyntax source, string method, IReadOnlyList<string> namespaces, MapCallKind kind)
    {
        ExpressionSyntax receiver = source.WithoutTrivia() is var bare && NeedsParentheses(bare) ? SyntaxFactory.ParenthesizedExpression(bare) : bare;
        if (kind is not (MapCallKind.Conditional or MapCallKind.Suppressed))
        {
            return PatternResult.Fixed(
                SyntaxFactory.InvocationExpression(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, receiver, SyntaxFactory.IdentifierName(method))),
                namespaces);
        }

        ExpressionSyntax call = SyntaxFactory.ConditionalAccessExpression(receiver,
            SyntaxFactory.InvocationExpression(SyntaxFactory.MemberBindingExpression(SyntaxFactory.IdentifierName(method))));
        if (kind == MapCallKind.Suppressed)
        {
            call = SyntaxFactory.PostfixUnaryExpression(SyntaxKind.SuppressNullableWarningExpression, call);
        }

        return PatternResult.Fixed(
            node.Parent is MemberAccessExpressionSyntax or ElementAccessExpressionSyntax or PostfixUnaryExpressionSyntax
                ? SyntaxFactory.ParenthesizedExpression(call)
                : call,
            namespaces);
    }

    // Measured (TAAK-271, msb): without any CreateMap for the pair AutoMapper throws a missing-type-map error at run time.
    private static string NoMapAnywhere(ITypeSymbol source, ITypeSymbol destination) =>
        $"No CreateMap<{source.Name}, {destination.Name}> in this project or the projects it uses, and no {source.Name}.To{destination.Name}() exists. "
        + "Unless AutoMapper finds the map another way (an [AutoMap] attribute, a map of a base type), it throws a missing-type-map error here too: "
        + "the call is broken in the source, so this stays manual (principally); fix the call or add the map by hand.";

    private static string NoMap(ITypeSymbol source, ITypeSymbol destination) =>
        $"No CreateMap<{source.Name}, {destination.Name}> in a profile this fix rewrites, and no {source.Name}.To{destination.Name}() exists; migrate the call by hand.";

    // The receiver is dropped: only a name, this.name or a chain of names may go.
    internal static bool HasNoSideEffects(ExpressionSyntax receiver) => receiver switch
    {
        IdentifierNameSyntax or ThisExpressionSyntax or BaseExpressionSyntax or PredefinedTypeSyntax => true,
        MemberAccessExpressionSyntax { Expression: var inner } => HasNoSideEffects(inner),
        ParenthesizedExpressionSyntax { Expression: var inner } => HasNoSideEffects(inner),
        _ => false,
    };

    internal static bool NeedsParentheses(ExpressionSyntax expression) => expression is not (IdentifierNameSyntax or MemberAccessExpressionSyntax or InvocationExpressionSyntax
        or ElementAccessExpressionSyntax or ParenthesizedExpressionSyntax or ThisExpressionSyntax or LiteralExpressionSyntax or ObjectCreationExpressionSyntax
        or GenericNameSyntax or PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression });

    internal static bool IsCollection(ITypeSymbol type) =>
        type.SpecialType != SpecialType.System_String && (type is IArrayTypeSymbol || type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable));
}
