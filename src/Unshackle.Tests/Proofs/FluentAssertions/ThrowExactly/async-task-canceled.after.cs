using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class AsyncTaskCanceledCase
{
    public async Task Test(Func<Task> action)
    {
        (await Should.ThrowAsync<TaskCanceledException>(action)).ShouldBeOfType<TaskCanceledException>();
    }
}
