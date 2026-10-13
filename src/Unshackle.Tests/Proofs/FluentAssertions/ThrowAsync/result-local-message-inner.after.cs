using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class ResultLocalMessageInnerCase
{
    public async Task Test(Func<Task> act)
    {
        var ex = await Should.ThrowAsync<InvalidOperationException>(act);
        ex.Message.ShouldBe("The circuit is now open and is not allowing calls.", Shouldly.StringCompareShould.IgnoreCase);
        ex.InnerException.ShouldNotBeNull().ShouldBeAssignableTo<ArgumentException>();
    }
}
