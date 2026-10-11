using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class BecauseCase
{
    public async Task Test(Func<Task> act)
    {
        await Should.ThrowAsync<InvalidOperationException>(act, "a");
    }
}
