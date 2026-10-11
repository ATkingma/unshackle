using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class MessageExactCase
{
    public async Task Test(Func<Task> act)
    {
        (await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldBe("Order shipped", Shouldly.StringCompareShould.IgnoreCase);
    }
}
