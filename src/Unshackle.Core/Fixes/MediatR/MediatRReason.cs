namespace Unshackle.Core.Fixes.MediatR;

/// <summary>Reason codes of MediatR Manual results (council round 10, C1), on <see cref="Models.ManualResult.Code"/>.</summary>
public enum MediatRReason
{
    /// <summary>The assembly an AddMediatR scan gets from a parameter or type parameter is not statically known (council round 8, A2).</summary>
    AssemblyNotStaticallyKnown,
}
