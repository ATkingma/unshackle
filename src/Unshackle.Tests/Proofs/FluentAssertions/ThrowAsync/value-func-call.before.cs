using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ValueFuncCallCase
{
    public async Task Test()
    {
        await Save().Should().ThrowAsync<InvalidOperationException>();
    }

    private static Func<Task<int>> Save() => () => Task.FromException<int>(new InvalidOperationException());
}
