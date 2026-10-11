using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class AggregateWaitCase
{
    public void Test(Task task)
    {
        Action act = () => task.Wait();
        Should.Throw<InvalidOperationException>(act).ShouldBeOfType<InvalidOperationException>();
    }
}
