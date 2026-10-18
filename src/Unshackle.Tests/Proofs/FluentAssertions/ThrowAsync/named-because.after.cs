using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class NamedBecauseCase
{
    public async Task Test(Func<Task> act)
    {
        await Should.ThrowAsync<InvalidOperationException>(act, "it is invalid");
    }
}
