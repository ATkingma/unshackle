using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AsyncTaskCanceledCase
{
    public async Task Test(Func<Task> action)
    {
        await action.Should().ThrowExactlyAsync<TaskCanceledException>();
    }
}
