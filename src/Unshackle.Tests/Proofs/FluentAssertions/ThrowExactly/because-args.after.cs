using System;
using Shouldly;

namespace Proofs;

public class BecauseArgsCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act, string.Format(System.Globalization.CultureInfo.InvariantCulture, "a {0}", 1)).ShouldBeOfType<InvalidOperationException>();
    }
}
