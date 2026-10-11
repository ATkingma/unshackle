using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class LambdaCase
{
    public void Test(List<int> x)
    {
        System.Action act = () => x.ShouldContain(1);
    }
}
