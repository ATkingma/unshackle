using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

// Shape from Steeltoe (Apache-2.0): RemoteLoadAsync_HostTimesOut, cancelled task with an inner TimeoutException (TAAK-221).
public class TaskCanceledInnerTimeoutCase
{
    public async Task Test(Func<Task> action)
    {
        (await action.Should().ThrowExactlyAsync<TaskCanceledException>()).WithInnerException<TimeoutException>();
    }
}
