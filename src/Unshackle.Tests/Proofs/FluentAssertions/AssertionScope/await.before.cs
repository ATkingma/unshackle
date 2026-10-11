using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class Case
{
    public async Task Test(int x, Func<Task> act)
    {
        using (new AssertionScope())
        {
            x.Should().Be(1);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}
