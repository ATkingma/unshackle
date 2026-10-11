using FluentAssertions;
using FluentAssertions.Execution;
using System;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test(Func<Task> act)
    {
        using var scope = new AssertionScope();
        await act.Should().NotThrowAsync();
    }
}
