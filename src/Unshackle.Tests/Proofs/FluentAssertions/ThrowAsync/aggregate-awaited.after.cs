using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class AggregateAwaitedCase
{
    public async Task Test(Func<Task<string>> work)
    {
        Func<Task> act = async () => await work();
        await Should.ThrowAsync<InvalidOperationException>(act);
    }
}
