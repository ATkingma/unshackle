using Shouldly;
using System.Collections.Generic;

namespace Proofs;

public class ReceiverParens
{
    public void Test(List<int>? a, List<int> b)
    {
        (a ?? b).Count.ShouldBe(1);
    }
}
