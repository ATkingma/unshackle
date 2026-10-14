using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

// Shape from Akka.NET (Apache-2.0): Streams specs block on .Result of the lambda parameter (TAAK-220).
public class LambdaParameterResultCase
{
    public void Test(Task<int> future)
    {
        future.Invoking(f => f.Result).Should().Throw<InvalidOperationException>();
    }
}
