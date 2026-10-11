using System;
using Shouldly;

namespace Proofs;

public class Case
{
    public void Test(Guid id, Guid? other)
    {
        id.ShouldNotBe(Guid.Empty);
        id.ShouldBe(Guid.Empty, "it is new");
        other.ShouldBe(Guid.Empty);
    }
}
