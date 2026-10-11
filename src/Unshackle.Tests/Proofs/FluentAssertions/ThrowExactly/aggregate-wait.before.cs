using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateWaitCase
{
    public void Test(Task task)
    {
        Action act = () => task.Wait();
        act.Should().ThrowExactly<InvalidOperationException>();
    }
}
