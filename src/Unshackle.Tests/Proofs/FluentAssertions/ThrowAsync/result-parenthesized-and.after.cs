using System;
using System.Threading.Tasks;
using FluentAssertions;
using Shouldly;

namespace Proofs;

public class ResultParenthesizedAndCase
{
    public async Task Test(Func<Task> act)
    {
        (await Should.ThrowAsync<ArgumentException>(act)).ParamName.Should().Be("id");
    }
}
