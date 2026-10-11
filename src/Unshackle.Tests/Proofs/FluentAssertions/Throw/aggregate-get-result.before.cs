using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateGetResultCase
{
    public void Test(Func<Task> work)
    {
        Action act = () => work().GetAwaiter().GetResult();
        act.Should().Throw<InvalidOperationException>();
    }
}
