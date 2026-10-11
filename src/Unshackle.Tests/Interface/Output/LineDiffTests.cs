using Shouldly;
using Unshackle.Interface.Output;
using Xunit;

namespace Unshackle.Tests.Interface.Output;

public class LineDiffTests
{
    [Fact]
    public void Compute_SameText_ReturnsNoLines()
    {
        // Arrange
        const string Text = "a\nb\n";

        // Act
        IReadOnlyList<DiffLine> lines = LineDiff.Compute(Text, Text);

        // Assert
        lines.ShouldBeEmpty();
    }

    [Fact]
    public void Compute_TwoSeparateChanges_ReturnsOnlyChangedLinesWithLineNumbers()
    {
        // Arrange
        const string Old = "using A;\nx.Should().Be(1);\nkeep\ny.Should().Be(2);\n";
        const string New = "using A;\nusing B;\nx.ShouldBe(1);\nkeep\ny.ShouldBe(2);\n";

        // Act
        IReadOnlyList<DiffLine> lines = LineDiff.Compute(Old, New);

        // Assert
        lines.Select(l => $"{(l.Kind == DiffKind.Removed ? '-' : '+')}{l.LineNumber} {l.Text}").ShouldBe(
        [
            "-2 x.Should().Be(1);",
            "+2 using B;",
            "+3 x.ShouldBe(1);",
            "-4 y.Should().Be(2);",
            "+5 y.ShouldBe(2);",
        ]);
    }

    [Fact]
    public void Compute_CrLfAgainstLf_TreatsLineEndingsAsEqual()
    {
        // Act
        IReadOnlyList<DiffLine> lines = LineDiff.Compute("a\r\nb\r\n", "a\nb\n");

        // Assert
        lines.ShouldBeEmpty();
    }
}
