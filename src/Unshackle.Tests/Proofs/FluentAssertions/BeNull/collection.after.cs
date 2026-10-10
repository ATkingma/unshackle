using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Collection
{
    public void Test(List<string>? list, int[]? array)
    {
        list.ShouldNotBeNull();
        array.ShouldBeNull();
    }
}
