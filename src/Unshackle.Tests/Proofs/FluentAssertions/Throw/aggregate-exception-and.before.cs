using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateExceptionAndCase
{
    public void Test(Func<Task> work, Exception expected)
    {
        Action action = () => work().Wait();
        action.Should().Throw<Exception>().And.Should().BeSameAs(expected);
    }
}
