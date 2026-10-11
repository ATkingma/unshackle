using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class ValueFuncCase
{
    public void Test(Func<int> func)
    {
        Should.NotThrow(func);
    }
}
