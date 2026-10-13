using FluentAssertions;
using System;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test(Breaker breaker)
    {
        await breaker.Awaiting(x => x.ExecuteAsync(() => Task.CompletedTask)).Should().NotThrowAsync();
    }
}

public class Breaker
{
    public Task ExecuteAsync(Func<Task> action) => action();
}
