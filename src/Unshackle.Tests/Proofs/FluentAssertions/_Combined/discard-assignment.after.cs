using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class Case
{
    public async Task Test(int a, List<int>? items, Func<Task> act)
    {
        a.ShouldBe(3);
        items.ShouldNotBeNull();
        items.Count.ShouldBe(2);
        await Should.ThrowAsync<InvalidOperationException>(act);
    }
}
