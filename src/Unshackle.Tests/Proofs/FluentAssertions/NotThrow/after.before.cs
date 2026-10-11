using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class AfterCase
{
    public void Test(Action act)
    {
        act.Should().NotThrowAfter(TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(10));
    }
}
