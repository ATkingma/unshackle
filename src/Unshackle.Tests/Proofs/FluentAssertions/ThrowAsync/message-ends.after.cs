using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class MessageEndsCase
{
    public async Task Test(Func<Task> act)
    {
        (await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldEndWith("shipped", Shouldly.Case.Insensitive);
    }
}
