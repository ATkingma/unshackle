using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class ValueFuncCase
{
    public async Task Test(Func<Task<int>> act)
    {
        await Should.ThrowAsync<InvalidOperationException>(() => act());
    }
}
