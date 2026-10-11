using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class MessageStartsCase
{
    public async Task Test(Func<Task> act)
    {
        (await Should.ThrowAsync<ArgumentException>(act)).Message.ShouldStartWith("order", Shouldly.Case.Insensitive);
    }
}
