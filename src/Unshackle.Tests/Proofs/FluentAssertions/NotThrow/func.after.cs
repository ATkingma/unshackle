using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class FuncCase
{
    public void Test(Func<string> func)
    {
        Should.NotThrow(func);
    }
}
