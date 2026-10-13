using Shouldly;
using System;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test(Breaker breaker)
    {
        await Should.NotThrowAsync(() => breaker.ExecuteAsync(() => Task.CompletedTask));
    }
}

public class Breaker
{
    public Task ExecuteAsync(Func<Task> action) => action();
}
