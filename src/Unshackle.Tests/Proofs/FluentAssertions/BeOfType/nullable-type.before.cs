#nullable enable
using System;
using FluentAssertions;

namespace Proofs;

public class NullableTypeCase
{
    public void Test(object value, string name)
    {
        value.Should().BeOfType(Type.GetType(name, throwOnError: true));
    }
}
