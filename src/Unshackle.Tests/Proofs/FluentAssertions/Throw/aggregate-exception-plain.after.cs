using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class AggregateExceptionPlainCase
{
    public void Test(Func<Task> work)
    {
        Action action = () => work().Wait();
        Should.Throw<Exception>(action);
    }
}
