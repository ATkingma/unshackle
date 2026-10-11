using System;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(Action act) => Should.NotThrow(act);
}
