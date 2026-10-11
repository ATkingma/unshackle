using System;
using FluentAssertions.Execution;
using Shouldly;

namespace Proofs;

public static class Retry
{
    public static void Within(int attempts, Action assertion)
    {
        for (int i = 1; ; i++)
        {
            try
            {
                assertion();
                return;
            }
            catch (Exception e)
            {
                if (!(e is AssertionFailedException || e is ShouldAssertException) || i == attempts)
                {
                    throw;
                }
            }
        }
    }
}
