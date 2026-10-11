using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class MessageExactCase
{
    public async Task Test(Func<Task> act)
    {
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("Order shipped");
    }
}
