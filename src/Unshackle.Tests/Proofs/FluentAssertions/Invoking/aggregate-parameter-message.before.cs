using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateParameterMessageCase
{
    public void Test(Task future)
    {
        future.Invoking(f => f.Wait()).Should().Throw<Exception>().WithMessage("Stream failed.");
    }
}
