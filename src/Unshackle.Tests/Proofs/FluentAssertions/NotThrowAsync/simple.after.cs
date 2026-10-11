using Shouldly;
using System;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test(Func<Task> act)
    {
        await Should.NotThrowAsync(act);
    }
}
