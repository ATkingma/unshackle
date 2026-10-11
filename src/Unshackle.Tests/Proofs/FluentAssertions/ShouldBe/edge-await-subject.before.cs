using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public async Task Test(Func<Task<int>> load)
    {
        (await load()).Should().Be(1);
    }
}
