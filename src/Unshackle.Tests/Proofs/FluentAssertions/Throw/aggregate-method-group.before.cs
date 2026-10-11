using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AggregateMethodGroupCase
{
    public void Test()
    {
        Action act = Run;
        act.Should().Throw<InvalidOperationException>();
    }

    private static void Run() => Parallel.ForEach(new[] { 1, 2 }, _ => throw new InvalidOperationException());
}
