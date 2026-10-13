using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ResultInnerAwaitedCase
{
    public async Task Test(Func<Task> act)
    {
        await act.Should().ThrowAsync<InvalidOperationException>().WithInnerException<InvalidOperationException, ArgumentException>();
    }
}
