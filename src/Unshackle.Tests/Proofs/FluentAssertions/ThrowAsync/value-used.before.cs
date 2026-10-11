using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ValueUsedCase
{
    public async Task Test(Func<Task> act)
    {
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
