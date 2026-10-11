using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class BeEmptyList
{
    public void Test(List<int> list)
    {
        list.ShouldBeEmpty();
    }
}
