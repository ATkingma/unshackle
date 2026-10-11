using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class StringCase
{
    public void Test(string text)
    {
        text.Should().Contain("ab");
    }
}
