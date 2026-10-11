using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class FuncTaskReferenceCase
{
    public async Task Test(Func<Task<string>> act)
    {
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
