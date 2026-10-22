namespace Unshackle.Core.Models;

/// <summary>
/// Reason codes (council round 10, C1) of a project-level Manual row that blocks the project's own file edits (council round 10, G4).
/// A "package kept" row that only follows from another block has no such code and blocks nothing on its own.
/// </summary>
public enum ProjectBlockingReason
{
    /// <summary>The project targets a framework the new library does not support (Polly, council round 10, P9).</summary>
    TargetFrameworkUnsupported,

    /// <summary>The package is declared in a shared Directory.*.props that a project not loaded, or one with blocking rows, imports too (G3).</summary>
    SharedDeclarationBlocked,

    /// <summary>The project resolves a dependency under a licence the migration must not pull in (EPPlus, council round 10, E7).</summary>
    LicensedDependency,
}
