using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class MessageExactBecauseCase
{
    public async Task Test(Func<Task> act)
    {
        (await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldBe("Order shipped", "b", Shouldly.StringCompareShould.IgnoreCase);
    }
}
