using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateAwaitedCase
{
    public async Task Test(Func<Task<string>> work)
    {
        Func<Task> act = async () => await work();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
