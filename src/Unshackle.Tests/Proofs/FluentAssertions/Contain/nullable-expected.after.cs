#nullable enable
using System;
using Shouldly;

namespace Proofs;

public class NullableExpectedCase
{
    public void Test(Exception ex)
    {
        ex.Message.ShouldContain(typeof(NullableExpectedCase).FullName!, Shouldly.Case.Sensitive);
    }
}
