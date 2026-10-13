using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ResultLocalAndCase
{
    public async Task Test(Func<Task> act, System.Threading.CancellationToken token)
    {
        var ex = await act.Should().ThrowAsync<OperationCanceledException>();
        ex.And.CancellationToken.Should().Be(token);
    }
}
