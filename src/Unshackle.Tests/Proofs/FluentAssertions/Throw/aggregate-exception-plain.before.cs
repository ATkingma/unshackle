using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateExceptionPlainCase
{
    public void Test(Func<Task> work)
    {
        Action action = () => work().Wait();
        action.Should().Throw<Exception>();
    }
}
