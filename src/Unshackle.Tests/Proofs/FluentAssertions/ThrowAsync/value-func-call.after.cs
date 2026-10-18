using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class ValueFuncCallCase
{
    public async Task Test()
    {
        var subject = Save();
        await Should.ThrowAsync<InvalidOperationException>(() => subject());
    }

    private static Func<Task<int>> Save() => () => Task.FromException<int>(new InvalidOperationException());
}
