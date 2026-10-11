using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public async Task Test(Func<Task<int>> load)
    {
        (await load()).ShouldBe(1);
    }
}
