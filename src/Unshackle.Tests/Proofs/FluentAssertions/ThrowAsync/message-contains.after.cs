using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class MessageContainsCase
{
    public async Task Test(Func<Task> act)
    {
        (await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldContain("at least one line", Shouldly.Case.Insensitive);
    }
}
