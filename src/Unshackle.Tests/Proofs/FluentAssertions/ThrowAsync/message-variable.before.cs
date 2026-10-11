using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class MessageVariableCase
{
    public async Task Test(Func<Task> act, string pattern)
    {
        await act.Should().ThrowAsync<ArgumentException>().WithMessage(pattern);
    }
}
