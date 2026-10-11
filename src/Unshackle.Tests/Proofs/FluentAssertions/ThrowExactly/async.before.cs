using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AsyncCase
{
    public async Task Test(Func<Task> act)
    {
        await act.Should().ThrowExactlyAsync<InvalidOperationException>();
    }
}
