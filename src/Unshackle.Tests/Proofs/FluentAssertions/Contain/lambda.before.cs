using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class LambdaCase
{
    public void Test(List<int> x)
    {
        System.Action act = () => x.Should().Contain(1);
    }
}
