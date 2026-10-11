using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AwaitingCase
{
    public async Task Test(string text)
    {
        await text.Awaiting(t => Task.CompletedTask).Should().ThrowAsync<InvalidOperationException>();
    }
}
