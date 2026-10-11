using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class AggregateExpectedCase
{
    public void Test(Func<Task> work)
    {
        Action act = () => work().Wait();
        Should.Throw<AggregateException>(act);
    }
}
