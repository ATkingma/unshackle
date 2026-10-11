using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class NoAwaitCase
{
    public async Task Test(Func<Task> act)
    {
        var task = act.Should().ThrowAsync<InvalidOperationException>();
    }
}
