using System;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Proofs;

// Shape from LiteDB (MIT): Int32IdAssertions_Tests, the helper with the FA assert sits one call deeper (TAAK-222).
public class HelperTwoLevelsCase
{
    public void Test(int[] actual)
    {
        Action candidate = () => IdAssertions.BeEquivalentTo(actual, 1, 2);
        candidate.Should().Throw<AssertionFailedException>();
    }
}

internal static class IdAssertions
{
    internal static void BeEquivalentTo(int[] actual, params int[] expected) => Check(actual, expected);

    private static void Check(int[] actual, int[] expected) => actual.Length.Should().Be(expected.Length);
}
