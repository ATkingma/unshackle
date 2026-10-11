using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class FuncCase
{
    public void Test(Func<string> func)
    {
        func.Should().NotThrow();
    }
}
