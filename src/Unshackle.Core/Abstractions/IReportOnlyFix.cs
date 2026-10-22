namespace Unshackle.Core.Abstractions;

/// <summary>
/// A fix that only takes inventory (council round 8, S2): its patterns return only Manual or Info results, the Fixer changes no
/// code, no usings and no packages for it, and the report says "inventory only". Implemented next to <see cref="IFix"/>, like
/// <see cref="IPartialFix"/>. A rewrite a pattern still returns is reported as Manual and not applied.
/// </summary>
public interface IReportOnlyFix
{
    /// <summary>Why this fix only reports, for the report header, for example <c>No free target ships on NuGet yet.</c></summary>
    string ReportOnlyReason { get; }
}
