using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ValueUsedCase
{
    public void Test(Func<string> func)
    {
        var value = func.Should().NotThrow().Which;
    }
}
