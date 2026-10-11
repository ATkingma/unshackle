using System;
using Shouldly;

namespace Proofs;

public class MessageBecauseArgsCase
{
    public void Test(Action act)
    {
        Should.Throw<InvalidOperationException>(act).Message.ShouldContain("a", Shouldly.Case.Insensitive, string.Format(System.Globalization.CultureInfo.InvariantCulture, "because {0}", 1));
    }
}
