using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ThrowExactlyCase
{
    public async Task Test(Func<Task> act)
    {
        await act.Should().ThrowExactlyAsync<InvalidOperationException>();
    }
}
