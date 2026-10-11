using System;
using Shouldly;

namespace Proofs;

public class LambdaCase
{
    public void Test(int x)
    {
        Action a = () => x.ShouldBeGreaterThan(1);
    }
}
