using FluentAssertions;
using System;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test(Breaker breaker)
    {
        await FluentActions.Awaiting(() => breaker.ExecuteAsync(() => Task.CompletedTask)).Should().NotThrowAsync("it is closed");
    }
}

public class Breaker
{
    public Task ExecuteAsync(Func<Task> action) => action();
}
