using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class ValueUsedCase
{
    public async Task Test(Func<Task> act)
    {
        var thrown = await Should.ThrowAsync<InvalidOperationException>(act);
    }
}
