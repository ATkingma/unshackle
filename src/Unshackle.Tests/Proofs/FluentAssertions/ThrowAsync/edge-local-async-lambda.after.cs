using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public async Task Test(Func<Task> run)
    {
        Func<Task> act = async () => await run();
        await Should.ThrowAsync<InvalidOperationException>(act);
    }
}
