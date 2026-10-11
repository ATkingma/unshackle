using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Dictionary
{
    public void Test(Dictionary<string, int> map)
    {
        map.ShouldBeEmpty();
        map.ShouldNotBeEmpty();
    }
}
