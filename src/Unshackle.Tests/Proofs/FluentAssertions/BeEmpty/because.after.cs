using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Because
{
    public void Test(List<int> list, string text)
    {
        list.ShouldBeEmpty("fresh order");
        text.ShouldNotBeEmpty("must be filled");
    }
}
