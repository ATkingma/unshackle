using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public async Task Test(Func<Task> act)
    {
        await Should.ThrowAsync<InvalidOperationException>(act).ConfigureAwait(false);
    }
}
