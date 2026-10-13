using Shouldly;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test(Counter counter)
    {
        await Should.NotThrowAsync(() => counter.CountAsync());
    }
}

public class Counter
{
    public Task<int> CountAsync() => Task.FromResult(1);
}
