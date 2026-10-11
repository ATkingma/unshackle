using System;
using Shouldly;

namespace Proofs;

public class SignCase
{
    public void Test(int x, double d)
    {
        x.ShouldBePositive();
        d.ShouldBeNegative("it dropped");
    }
}
