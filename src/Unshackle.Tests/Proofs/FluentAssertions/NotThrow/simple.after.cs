using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class SimpleCase
{
    public void Test(Action act)
    {
        Should.NotThrow(act);
    }
}
