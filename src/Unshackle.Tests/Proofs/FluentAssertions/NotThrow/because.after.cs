using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class BecauseCase
{
    public void Test(Action act)
    {
        Should.NotThrow(act, "it is valid");
    }
}
