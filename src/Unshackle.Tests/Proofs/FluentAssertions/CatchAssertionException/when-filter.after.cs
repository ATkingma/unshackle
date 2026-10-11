using System;
using FluentAssertions.Execution;
using Shouldly;

namespace Proofs;

public static class Retry
{
    public static bool Passes(Action assertion)
    {
        try
        {
            assertion();
            return true;
        }
        catch (Exception e) when (e is AssertionFailedException || e is ShouldAssertException)
        {
            return false;
        }
    }
}
