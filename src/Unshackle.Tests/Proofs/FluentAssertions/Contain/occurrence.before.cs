using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class OccurrenceCase
{
    public void Test(string text)
    {
        text.Should().Contain("ab", Exactly.Twice());
    }
}
