using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class NotBeEmptyList
{
    public void Test(List<int> list)
    {
        list.ShouldNotBeEmpty();
    }
}
