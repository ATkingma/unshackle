using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public async Task Test(Func<Task> run)
    {
        Func<Task> act = async () => await run();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
