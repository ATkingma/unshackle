#nullable enable
using System;
using FluentAssertions;

namespace Proofs;

public class NullableExpectedCase
{
    public void Test(Exception ex)
    {
        ex.Message.Should().Contain(typeof(NullableExpectedCase).FullName);
    }
}
