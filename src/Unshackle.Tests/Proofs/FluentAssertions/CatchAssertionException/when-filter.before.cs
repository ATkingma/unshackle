using System;
using FluentAssertions.Execution;

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
        catch (Exception e) when (e is AssertionFailedException)
        {
            return false;
        }
    }
}
