using FluentAssertions;
using System;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test()
    {
        await Create().Awaiting(x => x.ExecuteAsync(() => Task.CompletedTask)).Should().NotThrowAsync();
    }

    private static Breaker Create() => new();
}

public class Breaker
{
    public Task ExecuteAsync(Func<Task> action) => action();
}
