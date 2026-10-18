using System;
using Shouldly;

namespace Proofs;

public class ValueFuncCase
{
    public void Test(Action act, Func<int> func)
    {
        Should.Throw<InvalidOperationException>(() => func());
    }
}
