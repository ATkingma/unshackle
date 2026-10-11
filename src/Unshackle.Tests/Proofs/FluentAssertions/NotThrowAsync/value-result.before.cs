using FluentAssertions;
using System;
using System.Threading.Tasks;

namespace Proofs;

public class Case
{
    public async Task Test(Func<Task<int>> act)
    {
        await act.Should().NotThrowAsync();
    }
}
