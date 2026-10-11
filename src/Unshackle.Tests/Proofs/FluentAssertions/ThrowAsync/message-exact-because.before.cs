using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class MessageExactBecauseCase
{
    public async Task Test(Func<Task> act)
    {
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Order shipped", "b");
    }
}
