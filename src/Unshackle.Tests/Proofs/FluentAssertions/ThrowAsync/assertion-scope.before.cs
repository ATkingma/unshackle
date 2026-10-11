using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class AssertionScopeCase
{
    public async Task Test(Func<Task> act)
    {
        using var scope = new AssertionScope();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
