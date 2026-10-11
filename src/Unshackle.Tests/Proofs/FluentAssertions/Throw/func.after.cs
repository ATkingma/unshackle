using System;
using Shouldly;

namespace Proofs;

public class FuncCase
{
    public void Test(Action act, Func<string> func)
    {
        Should.Throw<InvalidOperationException>(func);
    }
}
