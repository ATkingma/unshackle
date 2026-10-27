using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Unshackle.Tests.Helpers;

namespace Unshackle.Tests.Core.EfBulk;

// References for efbulk proofs. EF Core 10 (Microsoft.EntityFrameworkCore.Sqlite, already a test package) is real. The Z packages are
// not: Z.EntityFramework.Plus.EFCore depends on the paid Z.EntityFramework.Extensions.EFCore, which may not enter
// Directory.Packages.props. So both are stubs, compiled here with the real assembly names and the public signatures of the real
// nupkgs (checked against 10.105.9 by metadata inspection, TAAK-342 step 4). The fix matches by assembly name.
public static class EfBulkReferences
{
    // Plus' builder overloads take the Extensions types Z.EntityFramework.Extensions.BatchDelete/BatchUpdate, so the Plus stub
    // references the Extensions stub. The real packages also have overloads taking an IDictionary<string, object> or an ExpandoObject;
    // the stubs keep the factory, builder and Expression<Func<T, object>> (anonymous object) shapes, so overload resolution of the
    // proofs matches the real package.
    private const string PlusSource = """
        using System;
        using System.Collections;
        using System.Collections.Generic;
        using System.Linq;
        using System.Linq.Expressions;
        using System.Threading;
        using System.Threading.Tasks;
        using Z.EntityFramework.Extensions;

        namespace Z.EntityFramework.Plus
        {
            public static class BatchDeleteExtensions
            {
                public static int Delete<T>(this IQueryable<T> query) where T : class => throw new NotSupportedException();
                public static int Delete<T>(this IQueryable<T> query, Action<BatchDelete> batchDeleteBuilder) where T : class => throw new NotSupportedException();
                public static Task<int> DeleteAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
                public static Task<int> DeleteAsync<T>(this IQueryable<T> query, Action<BatchDelete> batchDeleteBuilder, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
            }

            public static class BatchUpdateExtensions
            {
                public static int Update<T>(this IQueryable<T> query, Expression<Func<T, T>> updateFactory) where T : class => throw new NotSupportedException();
                public static int Update<T>(this IQueryable<T> query, Expression<Func<T, T>> updateFactory, Action<BatchUpdate> batchUpdateBuilder) where T : class => throw new NotSupportedException();
                public static int Update<T>(this IQueryable<T> query, Expression<Func<T, object>> updateExpression) where T : class => throw new NotSupportedException();
                public static Task<int> UpdateAsync<T>(this IQueryable<T> query, Expression<Func<T, T>> updateFactory, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
                public static Task<int> UpdateAsync<T>(this IQueryable<T> query, Expression<Func<T, T>> updateFactory, Action<BatchUpdate> batchUpdateBuilder, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
                public static Task<int> UpdateAsync<T>(this IQueryable<T> query, Expression<Func<T, object>> updateExpression) where T : class => throw new NotSupportedException();
            }

            public abstract class BaseQueryFuture { }

            public class QueryFutureEnumerable<T> : BaseQueryFuture, IEnumerable<T>
            {
                public IEnumerator<T> GetEnumerator() => throw new NotSupportedException();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            public static class QueryFutureExtensions
            {
                public static QueryFutureEnumerable<T> Future<T>(this IQueryable<T> query) => throw new NotSupportedException();
            }
        }
        """;

    // Z.EntityFramework.Extensions declares its query extension classes in the global namespace (no using needed) and the builder
    // options in Z.EntityFramework.Extensions.
    private const string ExtensionsSource = """
        using System;
        using System.Linq;
        using System.Linq.Expressions;
        using System.Threading;
        using System.Threading.Tasks;
        using Z.EntityFramework.Extensions;

        namespace Z.EntityFramework.Extensions
        {
            public class BatchDelete { public int BatchSize { get; set; } public int BatchDelayInterval { get; set; } }
            public class BatchUpdate { public int? BatchSize { get; set; } }
        }

        public static class BatchDeleteExtensions
        {
            public static int DeleteFromQuery<T>(this IQueryable<T> query) where T : class => throw new NotSupportedException();
            public static int DeleteFromQuery<T>(this IQueryable<T> query, Action<BatchDelete> batchDeleteBuilder) where T : class => throw new NotSupportedException();
            public static Task<int> DeleteFromQueryAsync<T>(this IQueryable<T> query, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
            public static Task<int> DeleteFromQueryAsync<T>(this IQueryable<T> query, Action<BatchDelete> batchDeleteBuilder, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
        }

        public static class BatchUpdateExtensions
        {
            public static int UpdateFromQuery<T>(this IQueryable<T> query, Expression<Func<T, T>> updateFactory) where T : class => throw new NotSupportedException();
            public static int UpdateFromQuery<T>(this IQueryable<T> query, Expression<Func<T, T>> updateFactory, Action<BatchUpdate> batchUpdateBuilder) where T : class => throw new NotSupportedException();
            public static int UpdateFromQuery<T>(this IQueryable<T> query, Expression<Func<T, object>> updateExpression) where T : class => throw new NotSupportedException();
            public static Task<int> UpdateFromQueryAsync<T>(this IQueryable<T> query, Expression<Func<T, T>> updateFactory, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
            public static Task<int> UpdateFromQueryAsync<T>(this IQueryable<T> query, Expression<Func<T, T>> updateFactory, Action<BatchUpdate> batchUpdateBuilder, CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
            public static Task<int> UpdateFromQueryAsync<TEntity>(this IQueryable<TEntity> query, Expression<Func<TEntity, object>> updateExpression) where TEntity : class => throw new NotSupportedException();
        }
        """;

    // A user-code library with the same method names that is not EF (the scout's false positives: FreeSql, Azure Table helpers).
    public const string LookAlikeSource = """
        using System.Linq;

        namespace LookAlike
        {
            public static class QueryableDeletes
            {
                public static int Delete<T>(this IQueryable<T> query) => 0;
            }
        }
        """;

    public static MetadataReference Extensions { get; } = Stub("Z.EntityFramework.Extensions.EFCore", ExtensionsSource, []);

    public static MetadataReference Plus { get; } = Stub("Z.EntityFramework.Plus.EFCore", PlusSource, [Extensions]);

    // EF Core with its relational layer (ExecuteDelete/ExecuteUpdate) and the Microsoft.Extensions assemblies its public API names.
    public static IReadOnlyList<MetadataReference> EfCore { get; } =
    [
        .. Directory.EnumerateFiles(AppContext.BaseDirectory, "Microsoft.EntityFrameworkCore*.dll")
            .Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "Microsoft.Extensions.*.dll").Where(p => !p.Contains("Polly", StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)
            .Select(p => MetadataReference.CreateFromFile(p)),
    ];

    public static IReadOnlyList<MetadataReference> Before { get; } = [.. TestReferences.Net, .. EfCore, Plus, Extensions];

    public static IReadOnlyList<MetadataReference> After { get; } = [.. TestReferences.Net, .. EfCore];

    private static CompilationReference Stub(string assemblyName, string source, IReadOnlyList<MetadataReference> references) =>
        CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            [.. TestReferences.Net, .. references],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)).ToMetadataReference();
}
