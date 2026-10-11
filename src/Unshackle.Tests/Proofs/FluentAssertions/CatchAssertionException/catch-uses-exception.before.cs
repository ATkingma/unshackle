using System;
using FluentAssertions.Execution;

namespace Proofs;

public static class Retry
{
    public static string Failure(Action assertion)
    {
        try
        {
            assertion();
            return "";
        }
        catch (AssertionFailedException e)
        {
            return e.Message;
        }
    }
}
