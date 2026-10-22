using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Unshackle.Core.Fixes.MediatR;

/// <summary>
/// Resolves the assembly an AddMediatR scan gets from a parameter or a method type parameter through the callers in the solution
/// (council round 8, A2). A source resolves when:
/// <list type="number">
/// <item>its method is not visible outside its project, or its project is not packable (an executable or a test project);</item>
/// <item>every caller is a plain call in the solution that passes the literal <c>typeof(X).Assembly</c> (or names <c>X</c> as the explicit
/// type argument), all with X in the same assembly;</item>
/// <item>no caller hides in inactive <c>#if</c> code, a method group or a lambda that is not passed straight to a call;</item>
/// <item>that assembly is the scanning project or one of its references, where Mediator's source generator can see the handlers.</item>
/// </list>
/// Everything else stays unknown (<see cref="AssemblyResolution.AssemblyNotStaticallyKnown"/>).
/// </summary>
public static class AssemblyResolver
{
    // Assemblies a test project references: a test project is never packed.
    private static readonly HashSet<string> s_testFrameworks =
        new(["Microsoft.TestPlatform.CoreUtilities", "Microsoft.VisualStudio.TestPlatform.TestFramework", "xunit.core", "xunit.v3.core", "nunit.framework"], StringComparer.OrdinalIgnoreCase);

    /// <summary>Resolves each parameter and method type parameter the given dropped scans use.</summary>
    public static async Task<AssemblyResolution> ResolveAsync(
        IReadOnlyList<InvocationExpressionSyntax> dropped, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        List<ISymbol> sources = [.. Sources(dropped, model, cancellationToken)];
        if (sources.Count == 0)
        {
            return AssemblyResolution.None;
        }

        var resolved = new Dictionary<ISymbol, IAssemblySymbol>(SymbolEqualityComparer.Default);
        var unknown = new Dictionary<ISymbol, string>(SymbolEqualityComparer.Default);
        foreach (ISymbol source in sources)
        {
            (IAssemblySymbol? assembly, string? why) = await ResolveAsync(source, model, solution, cancellationToken);
            if (assembly is not null)
            {
                resolved[source] = assembly;
            }
            else
            {
                unknown[source] = why!;
            }
        }

        return new AssemblyResolution { Resolved = resolved, Unknown = unknown };
    }

    // Assembly parameters passed as a whole argument, and type parameters of a method named by typeof(T) or <T>; other parameters
    // (e.g. handlers.GetName()) and type parameters of a type are not followed and stay with the existing Manual checks.
    private static IEnumerable<ISymbol> Sources(IReadOnlyList<InvocationExpressionSyntax> dropped, SemanticModel model, CancellationToken cancellationToken)
    {
        IEnumerable<ISymbol> parameters = dropped.SelectMany(d => d.ArgumentList.Arguments)
            .Select(a => model.GetSymbolInfo(a.Expression, cancellationToken).Symbol)
            .OfType<IParameterSymbol>()
            .Where(p => p.Type.ToDisplayString() == "System.Reflection.Assembly" && !p.IsParams);
        IEnumerable<ISymbol> typeParameters = dropped.SelectMany(d => d.DescendantNodesAndSelf())
            .SelectMany(n => n switch
            {
                TypeOfExpressionSyntax t => [t.Type],
                GenericNameSyntax g => g.TypeArgumentList.Arguments,
                _ => (IEnumerable<TypeSyntax>)[],
            })
            .Select(t => model.GetTypeInfo(t, cancellationToken).Type)
            .OfType<ITypeParameterSymbol>()
            .Where(t => t.DeclaringMethod is not null);
        return parameters.Concat(typeParameters).Distinct(SymbolEqualityComparer.Default);
    }

    private static async Task<(IAssemblySymbol? Assembly, string? Why)> ResolveAsync(ISymbol source, SemanticModel model, Solution solution, CancellationToken cancellationToken)
    {
        IMethodSymbol method = (source as IParameterSymbol)?.ContainingSymbol as IMethodSymbol ?? ((ITypeParameterSymbol)source).DeclaringMethod!;
        string name = $"{method.ContainingType.Name}.{method.Name}";
        Project? project = solution.Projects.FirstOrDefault(p => p.AssemblyName == model.Compilation.AssemblyName);
        if (Patterns.AddMediatRPattern.IsVisibleOutside(method) && (project is null || IsPackable(project)))
        {
            return (null, $"{name} is public in a packable project, so callers outside the solution can pass any assembly");
        }

        if (await InactiveCallerAsync(method.Name, solution, cancellationToken) is string inactive)
        {
            return (null, $"{name} may be called in inactive #if code ({inactive})");
        }

        IEnumerable<ReferenceLocation> locations = (await SymbolFinder.FindReferencesAsync(method.OriginalDefinition, solution, cancellationToken))
            .SelectMany(r => r.Locations);
        string? assemblyName = null;
        foreach (ReferenceLocation location in locations)
        {
            string where = $"{location.Document.FilePath ?? location.Document.Name}:{location.Location.GetLineSpan().StartLinePosition.Line + 1}";
            SyntaxNode? root = await location.Document.GetSyntaxRootAsync(cancellationToken);
            SemanticModel? callModel = await location.Document.GetSemanticModelAsync(cancellationToken);
            if (root?.FindNode(location.Location.SourceSpan, getInnermostNodeForTie: true) is not SimpleNameSyntax called
                || Call(called) is not InvocationExpressionSyntax call
                || callModel is null)
            {
                return (null, $"{name} is used other than as a plain call ({where})");
            }

            if (call.Ancestors().OfType<AnonymousFunctionExpressionSyntax>().Any(l => l.Parent is not ArgumentSyntax))
            {
                return (null, $"{name} is called in a lambda that is not passed straight to a call ({where})");
            }

            if (Literal(source, called, call, callModel, cancellationToken) is not INamedTypeSymbol type)
            {
                return (null, $"the call at {where} does not pass a literal typeof(X).Assembly");
            }

            if (assemblyName is not null && assemblyName != type.ContainingAssembly.Name)
            {
                return (null, $"callers pass different assemblies ({assemblyName}, {type.ContainingAssembly.Name} at {where})");
            }

            assemblyName = type.ContainingAssembly.Name;
        }

        if (assemblyName is null)
        {
            return (null, $"{name} has no callers in the solution");
        }

        // Mediator's source generator only sees the scanning project and its references.
        IAssemblySymbol? visible = model.Compilation.Assembly.Name == assemblyName
            ? model.Compilation.Assembly
            : model.Compilation.SourceModule.ReferencedAssemblySymbols.FirstOrDefault(a => a.Name == assemblyName);
        return visible is null
            ? (null, $"every caller passes {assemblyName}, which {model.Compilation.AssemblyName} does not reference, so Mediator's source generator there cannot see its handlers")
            : (visible, null);
    }

    // The call a reference names: M(...), x.M(...) or x?.M(...); null for a method group, nameof or any other use.
    private static InvocationExpressionSyntax? Call(SimpleNameSyntax called) => called.Parent switch
    {
        InvocationExpressionSyntax invocation when invocation.Expression == called => invocation,
        MemberAccessExpressionSyntax member when member.Name == called && member.Parent is InvocationExpressionSyntax invocation && invocation.Expression == member => invocation,
        MemberBindingExpressionSyntax binding when binding.Name == called && binding.Parent is InvocationExpressionSyntax invocation && invocation.Expression == binding => invocation,
        _ => null,
    };

    // The X of typeof(X).Assembly passed for the parameter, or of the explicit type argument for the type parameter; null otherwise.
    private static INamedTypeSymbol? Literal(ISymbol source, SimpleNameSyntax called, InvocationExpressionSyntax call, SemanticModel model, CancellationToken cancellationToken)
    {
        TypeSyntax? literal = source switch
        {
            IParameterSymbol parameter => Patterns.AddMediatRPattern.Argument(call, model, parameter, cancellationToken) is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Assembly", Expression: TypeOfExpressionSyntax type }
                ? type.Type
                : null,
            ITypeParameterSymbol typeParameter => called is GenericNameSyntax generic && typeParameter.Ordinal < generic.TypeArgumentList.Arguments.Count
                ? generic.TypeArgumentList.Arguments[typeParameter.Ordinal]
                : null,
            _ => null,
        };
        return literal is null ? null : model.GetTypeInfo(literal, cancellationToken).Type as INamedTypeSymbol;
    }

    // A file of the solution whose inactive #if code names the method: a caller the compiler, and so SymbolFinder, does not see.
    private static async Task<string?> InactiveCallerAsync(string method, Solution solution, CancellationToken cancellationToken)
    {
        foreach (Document document in solution.Projects.Where(p => p.Language == LanguageNames.CSharp).SelectMany(p => p.Documents))
        {
            SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
            if (root is not null && root.DescendantTrivia().Any(t => t.IsKind(SyntaxKind.DisabledTextTrivia) && t.ToString().Contains(method, StringComparison.Ordinal)))
            {
                return document.FilePath ?? document.Name;
            }
        }

        return null;
    }

    // An SDK library is packable by default; an executable or a test project is not. IsPackable itself is an MSBuild property the
    // workspace does not expose, so a library with IsPackable=false still counts as packable (Manual rather than a wrong Fixed).
    private static bool IsPackable(Project project) =>
        project.CompilationOptions?.OutputKind is OutputKind.DynamicallyLinkedLibrary or null
        && !project.MetadataReferences.OfType<PortableExecutableReference>()
            .Any(r => s_testFrameworks.Contains(Path.GetFileNameWithoutExtension(r.FilePath ?? r.Display ?? string.Empty)));
}
