using System;
using System.Threading.Tasks;
using FluentAssertions;
using Shouldly;

namespace Proofs;

public class ResultLocalAndCase
{
    public async Task Test(Func<Task> act, System.Threading.CancellationToken token)
    {
        var ex = await Should.ThrowAsync<OperationCanceledException>(act);
        ex.CancellationToken.Should().Be(token);
    }
}
