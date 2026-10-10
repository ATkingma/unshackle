using System;
using Shouldly;

namespace Proofs;

public class Lambda
{
    public void Test(string? text)
    {
        Action act = () => text.ShouldBeNull();
    }
}
