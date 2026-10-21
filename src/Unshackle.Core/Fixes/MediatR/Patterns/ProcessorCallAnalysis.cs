namespace Unshackle.Core.Fixes.MediatR.Patterns;

/// <summary>The outcome of analysing one Process call: a problem for the report, or which processor kind and response type it has.</summary>
internal class ProcessorCallAnalysis
{
    public required string? Problem { get; init; }

    public bool IsPre { get; init; }

    public string ResponseType { get; init; } = "";
}
