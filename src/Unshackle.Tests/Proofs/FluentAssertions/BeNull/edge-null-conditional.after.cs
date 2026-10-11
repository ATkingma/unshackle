using System;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(Uri? uri)
    {
        uri?.Host.ShouldBeNull();
    }
}
