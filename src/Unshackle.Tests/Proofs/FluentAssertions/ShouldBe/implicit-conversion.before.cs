using System;
using FluentAssertions;

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

public class ShouldBeImplicitCase
{
    public void Test(Doc doc, Doc[] docs)
    {
        doc.Should().Be(42);
        doc.Should().Be("John", "name");
        docs[0].Should().Be(1);
        doc.Should().Be(doc);
    }
}
