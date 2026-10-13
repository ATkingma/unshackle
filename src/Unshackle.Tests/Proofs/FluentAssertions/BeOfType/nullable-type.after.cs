#nullable enable
using System;
using Shouldly;

namespace Proofs;

public class NullableTypeCase
{
    public void Test(object value, string name)
    {
        value.ShouldBeOfType(Type.GetType(name, throwOnError: true)!);
    }
}
