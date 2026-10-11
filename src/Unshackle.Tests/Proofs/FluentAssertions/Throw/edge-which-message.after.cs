using System;
using Shouldly;

namespace Proofs;

public class EdgeCase
{
    public void Test(Action act)
    {
        string message = Should.Throw<Exception>(act).Message;
    }
}
