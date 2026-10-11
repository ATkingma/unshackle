using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class MessageContainsCase
{
    public async Task Test(Func<Task> act)
    {
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*at least one line*");
    }
}
