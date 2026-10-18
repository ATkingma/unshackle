using FluentAssertions;
using System;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test(Func<Task> act)
    {
        _ = await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
