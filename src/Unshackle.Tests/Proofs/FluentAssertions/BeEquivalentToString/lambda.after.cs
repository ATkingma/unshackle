using System;
using Shouldly;

namespace Proofs;

public class LambdaCase
{
    public void Test(string s)
    {
        Action act = () => s.ShouldBe("ab", StringCompareShould.IgnoreCase);
    }
}
