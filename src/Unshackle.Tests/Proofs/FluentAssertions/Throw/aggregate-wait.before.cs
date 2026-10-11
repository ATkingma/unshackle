using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateWaitCase
{
    public void Test(Func<Task> work)
    {
        Action act = () => work().Wait();
        act.Should().Throw<InvalidOperationException>();
    }
}
