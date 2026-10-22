namespace Unshackle.Core.Models;

/// <summary>What an <see cref="InfoResult"/> row is (council round 10, G1).</summary>
public enum InfoKind
{
    /// <summary>A note about the code or a project, for example a row of a report-only inventory or a fix-level Info row.</summary>
    Note = 0,

    /// <summary>The header row of a report-only fix ("inventory only"): not a finding, so every count leaves it out.</summary>
    Header = 1,
}
