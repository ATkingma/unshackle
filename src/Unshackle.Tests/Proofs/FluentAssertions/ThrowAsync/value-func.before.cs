using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ValueFuncCase
{
    public async Task Test(Func<Task<int>> act)
    {
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
