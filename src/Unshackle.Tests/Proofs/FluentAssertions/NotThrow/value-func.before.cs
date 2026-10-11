using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ValueFuncCase
{
    public void Test(Func<int> func)
    {
        func.Should().NotThrow();
    }
}
