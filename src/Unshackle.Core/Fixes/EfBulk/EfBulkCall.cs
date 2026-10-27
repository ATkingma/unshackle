using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Unshackle.Core.Fixes.EfBulk;

// Recognises a batch call of Z.EntityFramework.Plus / Z.EntityFramework.Extensions on an IQueryable<T>. Matching is by the method's
// assembly, never by name alone: FreeSql, Azure Table helpers and LanguageExt have methods with the same names (scout report).
internal static class EfBulkCall
{
    public const string PlusAssembly = "Z.EntityFramework.Plus.EFCore";

    public const string ExtensionsAssembly = "Z.EntityFramework.Extensions.EFCore";

    public const string EfCoreNamespace = "Microsoft.EntityFrameworkCore";

    // EF Core 7-9 declare ExecuteDelete/ExecuteUpdate in the relational layer, EF Core 10 in EF Core itself.
    private static readonly string[] s_nativeHosts =
        ["Microsoft.EntityFrameworkCore.RelationalQueryableExtensions", "Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions"];

    private static readonly HashSet<string> s_assemblies = new([PlusAssembly, ExtensionsAssembly], StringComparer.OrdinalIgnoreCase);

    public static SimpleNameSyntax? Name(InvocationExpressionSyntax invocation) => invocation.Expression switch
    {
        MemberAccessExpressionSyntax access => access.Name,
        MemberBindingExpressionSyntax binding => binding.Name,
        _ => null,
    };

    // The old extension method when the invocation is one of names and binds to a Z assembly as an IQueryable<T> extension.
    public static IMethodSymbol? OldMethod(InvocationExpressionSyntax invocation, SemanticModel model, IReadOnlyDictionary<string, string> names)
    {
        if (Name(invocation) is not { } name || !names.ContainsKey(name.Identifier.ValueText))
        {
            return null;
        }

        return model.GetSymbolInfo(invocation).Symbol is IMethodSymbol { IsExtensionMethod: true } method
            && method.ContainingAssembly is { } assembly && s_assemblies.Contains(assembly.Name)
            && (method.ReducedFrom ?? method).Parameters is [{ Type: INamedTypeSymbol { MetadataName: "IQueryable`1" } }, ..]
                ? method
                : null;
    }

    // EF6 and EF Core 3-6 lack ExecuteDelete/ExecuteUpdate (TAAK-342 council question 3: such a project is Manual).
    public static bool HasNative(SemanticModel model, string method) =>
        s_nativeHosts.Any(host => model.Compilation.GetTypeByMetadataName(host) is { } type && !type.GetMembers(method).IsEmpty);

    public static bool IsCancellationToken(ArgumentSyntax argument, SemanticModel model) =>
        model.GetTypeInfo(argument.Expression).ConvertedType is { Name: "CancellationToken", ContainingNamespace: { } ns }
        && ns.ToDisplayString() == "System.Threading";

    // The same call with another method name; type arguments and the receiver stay.
    public static InvocationExpressionSyntax Renamed(InvocationExpressionSyntax invocation, SimpleNameSyntax name, string newName) =>
        invocation.ReplaceNode(name, name.WithIdentifier(SyntaxFactory.Identifier(newName)));
}
