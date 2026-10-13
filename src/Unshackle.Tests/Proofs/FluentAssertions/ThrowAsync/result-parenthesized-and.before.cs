using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ResultParenthesizedAndCase
{
    public async Task Test(Func<Task> act)
    {
        (await act.Should().ThrowAsync<ArgumentException>()).And.ParamName.Should().Be("id");
    }
}
