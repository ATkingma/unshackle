using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    private List<int> Load() => new();

    public void Test(int y)
    {
        var subject = Load();
        subject.ShouldNotBeEmpty();
        subject.ShouldAllBe(i => i > y);
    }
}
