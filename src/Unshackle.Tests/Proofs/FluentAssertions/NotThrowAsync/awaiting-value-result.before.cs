using FluentAssertions;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test(Counter counter)
    {
        await counter.Awaiting(c => c.CountAsync()).Should().NotThrowAsync();
    }
}

public class Counter
{
    public Task<int> CountAsync() => Task.FromResult(1);
}
