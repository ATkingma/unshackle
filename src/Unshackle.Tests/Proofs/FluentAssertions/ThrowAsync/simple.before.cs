using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class SimpleCase
{
    public async Task Test(Func<Task> act)
    {
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
