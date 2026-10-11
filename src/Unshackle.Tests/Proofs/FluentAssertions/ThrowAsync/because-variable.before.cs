using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class BecauseVariableCase
{
    public async Task Test(Func<Task> act, string why)
    {
        await act.Should().ThrowAsync<InvalidOperationException>(why, 1);
    }
}
