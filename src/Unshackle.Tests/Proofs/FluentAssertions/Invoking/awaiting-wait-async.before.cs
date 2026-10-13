using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AwaitingWaitAsyncCase
{
    public async Task Test(Task task)
    {
        await task.Awaiting(t => t.WaitAsync(TimeSpan.FromSeconds(3))).Should().ThrowAsync<InvalidOperationException>();
    }
}
