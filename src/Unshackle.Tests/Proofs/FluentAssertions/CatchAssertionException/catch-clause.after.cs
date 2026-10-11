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
        catch (AssertionFailedException)
        {
            return false;
        }
        catch (ShouldAssertException)
        {
            return false;
        }
    }
}
