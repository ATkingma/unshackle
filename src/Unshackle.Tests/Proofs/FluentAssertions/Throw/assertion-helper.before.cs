using System;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

public class AssertionHelperCase
{
    public void Test(int[] actual)
    {
        Action candidate = () => IdAssertions.AllPositive(actual);
        candidate.Should().Throw<AssertionFailedException>();
    }
}

internal static class IdAssertions
{
    internal static void AllPositive(int[] ids)
    {
        Array.TrueForAll(ids, id => id > 0).Should().BeTrue("ids must be positive");
    }
}
