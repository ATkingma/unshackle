using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class ListCase
{
    public void Test(List<int> x)
    {
        x.ShouldContain(2);
    }
}
