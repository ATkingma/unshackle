using System;
using Shouldly;

namespace Proofs;

public class LambdaCase
{
    public void Test(Action act)
    {
        Action run = () => Should.Throw<InvalidOperationException>(act);
    }
}
