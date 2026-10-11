using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class MessageBecauseCase
{
    public async Task Test(Func<Task> act)
    {
        (await Should.ThrowAsync<ArgumentException>(act, "a")).Message.ShouldContain("shipped", Shouldly.Case.Insensitive, "b");
    }
}
