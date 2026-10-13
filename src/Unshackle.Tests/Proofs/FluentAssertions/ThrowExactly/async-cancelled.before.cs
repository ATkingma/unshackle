using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AsyncCancelledCase
{
    public async Task Test(Func<Task> action)
    {
        await action.Should().ThrowExactlyAsync<OperationCanceledException>();
    }
}
