using Shouldly;
using System;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test(Breaker breaker)
    {
        await Should.NotThrowAsync(() => breaker.ExecuteAsync(() => Task.CompletedTask), "it is closed");
    }
}

public class Breaker
{
    public Task ExecuteAsync(Func<Task> action) => action();
}
