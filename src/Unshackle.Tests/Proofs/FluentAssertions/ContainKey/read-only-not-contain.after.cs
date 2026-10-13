using System.Collections.Generic;
using Shouldly;

namespace Proofs;

public class Case
{
    public IReadOnlyDictionary<string, string?> Metadata { get; } = new Dictionary<string, string?>();

    public void Test()
    {
        Metadata.ContainsKey("a").ShouldBeFalse("it was removed");
    }
}
