using System;
using Shouldly;

namespace Proofs;

public class SignCase
{
    public void Test(int x, double d)
    {
        x.ShouldBePositive();
        d.ShouldBePositive("it dropped");
    }
}
