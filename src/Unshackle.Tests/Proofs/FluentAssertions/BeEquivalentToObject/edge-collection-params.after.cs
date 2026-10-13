using System.Collections.Generic;
using System.Linq;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(List<int> numbers)
    {
        numbers.Select(n => n.ToString()).ShouldBe(new string[] { "1", "2" }, ignoreOrder: true);
    }
}
