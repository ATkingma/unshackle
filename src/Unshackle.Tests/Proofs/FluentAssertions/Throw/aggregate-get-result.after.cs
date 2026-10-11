using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class AggregateGetResultCase
{
    public void Test(Func<Task> work)
    {
        Action act = () => work().GetAwaiter().GetResult();
        Should.Throw<InvalidOperationException>(act);
    }
}
