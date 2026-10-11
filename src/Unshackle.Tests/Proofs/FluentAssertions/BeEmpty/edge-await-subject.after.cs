using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public async Task Test(Func<Task<List<int>>> load)
    {
        (await load()).ShouldBeEmpty();
    }
}
