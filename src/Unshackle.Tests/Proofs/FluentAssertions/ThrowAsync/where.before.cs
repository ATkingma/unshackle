using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class WhereCase
{
    public async Task Test(Func<Task> act)
    {
        await act.Should().ThrowAsync<InvalidOperationException>().Where(e => e.Message.Length > 0);
    }
}
