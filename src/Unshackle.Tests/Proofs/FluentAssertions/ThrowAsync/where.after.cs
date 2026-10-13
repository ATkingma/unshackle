using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class WhereCase
{
    public async Task Test(Func<Task> act)
    {
        var exception = await Should.ThrowAsync<InvalidOperationException>(act);
        (exception.Message.Length > 0).ShouldBeTrue();
    }
}
