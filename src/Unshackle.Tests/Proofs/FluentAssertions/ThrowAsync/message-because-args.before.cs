using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class MessageBecauseArgsCase
{
    public async Task Test(Func<Task> act)
    {
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*x*", "a {0}", 1);
    }
}
