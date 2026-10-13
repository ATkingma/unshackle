using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateParameterAndCase
{
    public void Test(Task task, Exception expected)
    {
        task.Invoking(_ => _.Wait(TimeSpan.FromSeconds(3))).Should().Throw<Exception>().And.Should().Be(expected);
    }
}
