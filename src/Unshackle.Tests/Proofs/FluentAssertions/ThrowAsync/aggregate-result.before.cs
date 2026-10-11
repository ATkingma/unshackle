using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateResultCase
{
    public async Task Test(Func<Task<string>> work)
    {
        Func<Task> act = () => Task.FromResult(work().Result);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
