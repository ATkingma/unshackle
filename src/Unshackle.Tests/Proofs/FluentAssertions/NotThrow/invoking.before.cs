using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class InvokingCase
{
    public void Test(object o)
    {
        o.Invoking(x => x.ToString()).Should().NotThrow();
    }
}
