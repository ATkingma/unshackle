using Shouldly;
using Unshackle.Core.Abstractions;
using Unshackle.Core.Fixes.AutoMapper;
using Unshackle.Core.Fixes.FluentAssertions;
using Unshackle.Core.Fixes.MediatR;
using Unshackle.Interface.Commands;
using Unshackle.Tests.Helpers;
using Xunit;

namespace Unshackle.Tests.Interface.Commands;

// docs/patterns.md is the output of `unshackle list --markdown` below its header: a new or changed pattern must update it.
public class PatternDocTests
{
    public const string Marker = "<!-- generated: unshackle list --markdown -->";

    [Fact]
    public void PatternsDoc_MatchesListMarkdown()
    {
        // Arrange
        IFix[] fixes = [new FluentAssertionsFix(TestReferences.ShouldlyAssembly), new MediatRFix(), new AutoMapperFix()];
        string doc = File.ReadAllText(Path.Combine(RepositoryRoot.Path, "docs", "patterns.md")).ReplaceLineEndings("\n");

        // Act
        string generated = doc[(doc.IndexOf(Marker, StringComparison.Ordinal) + Marker.Length)..].Trim();

        // Assert
        generated.ShouldBe(ListCommand.ToMarkdown(fixes), "regenerate the part below the marker in docs/patterns.md with: unshackle list --markdown");
    }
}
