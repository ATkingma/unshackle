using System.Collections.Generic;
using FluentAssertions;

namespace Proofs;

public class Case
{
    public IReadOnlyDictionary<string, string?> Metadata { get; } = new Dictionary<string, string?>();

    public void Test()
    {
        Metadata.Should().NotContainKey("a", "it was removed");
    }
}
