using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public void Test(Maybe<int> maybe)
    {
        var context = 5;
        var maybe2 = maybe.Bind(
            (value, ctx) =>
            {
                ctx.Should().Be(context);
                return Maybe.From(value);
            },
            context);
        maybe2.HasValue.Should().BeTrue();
    }
}

public readonly struct Maybe<T>
{
    public bool HasValue => true;
}

public static class Maybe
{
    public static Maybe<T> From<T>(T value) => default;
}

public static class MaybeExtensions
{
    public static Maybe<K> Bind<T, K, TContext>(this Maybe<T> maybe, Func<T, TContext, Maybe<K>> selector, TContext context) => default;

    public static Task<Maybe<K>> Bind<T, K, TContext>(this Maybe<T> maybe, Func<T, TContext, Task<Maybe<K>>> selector, TContext context) =>
        Task.FromResult(default(Maybe<K>));
}
