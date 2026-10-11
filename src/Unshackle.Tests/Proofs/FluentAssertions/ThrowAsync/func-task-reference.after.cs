using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class FuncTaskReferenceCase
{
    public async Task Test(Func<Task<string>> act)
    {
        await Should.ThrowAsync<InvalidOperationException>(act);
    }
}
