using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class EdgeCase
{
    public async Task Test(Func<Task<List<int>>> load)
    {
        (await load()).Should().BeEmpty();
    }
}
