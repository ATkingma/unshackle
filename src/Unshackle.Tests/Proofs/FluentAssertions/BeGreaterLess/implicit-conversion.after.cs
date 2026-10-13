using System;
using Shouldly;

namespace Proofs;

public class Doc : IComparable<Doc>
{
    public int Value { get; init; }

    public static implicit operator Doc(int value) => new Doc { Value = value };

    public static implicit operator Doc(string text) => new Doc { Value = text.Length };

    public override bool Equals(object? obj) => obj is Doc other && other.Value == Value;

    public override int GetHashCode() => Value;

    public int CompareTo(Doc? other) => Value.CompareTo(other?.Value ?? 0);
}

public class GreaterLessImplicitCase
{
    public void Test(Doc doc, Doc[] docs)
    {
        doc.ShouldBeGreaterThan((Doc)0);
        doc.ShouldBeLessThanOrEqualTo((Doc)7, "small");
    }
}
