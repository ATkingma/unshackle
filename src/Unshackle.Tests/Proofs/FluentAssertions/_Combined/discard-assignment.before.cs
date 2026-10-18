using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public async Task Test(int a, List<int>? items, Func<Task> act)
    {
        _ = a.Should().Be(3);
        _ = items.Should().NotBeNull().And.HaveCount(2);
        _ = await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
