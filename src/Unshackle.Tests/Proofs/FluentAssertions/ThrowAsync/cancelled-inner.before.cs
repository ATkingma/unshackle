using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class CancelledInnerCase
{
    public async Task Test(Func<Task> action)
    {
        (await action.Should().ThrowAsync<TaskCanceledException>()).WithInnerException<TimeoutException>();
    }
}
