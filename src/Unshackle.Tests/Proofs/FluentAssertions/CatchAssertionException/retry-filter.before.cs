using System;
using FluentAssertions.Execution;

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
                if (!(e is AssertionFailedException) || i == attempts)
                {
                    throw;
                }
            }
        }
    }
}
