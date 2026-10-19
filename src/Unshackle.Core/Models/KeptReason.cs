namespace Unshackle.Core.Models;

/// <summary>
/// Why a finding stays on the old library while the rest of the fix goes ahead (council round 3, Q2). A <see cref="ManualResult"/> with a
/// kept reason does not undo the fix; the report renderer prints the "Stays on AutoMapper: " prefix in front of its reason.
/// </summary>
public enum KeptReason
{
    /// <summary>Not a kept finding: an ordinary Manual result.</summary>
    None = 0,

    /// <summary>Rule 1: no AddAutoMapper call in the project of the lookup or a project that depends on it.</summary>
    NoRegistration = 1,

    /// <summary>Rule 2: every AddAutoMapper call is conditional, or sits in a lambda that may run later or not at all.</summary>
    ConditionalOrDeferredRegistration = 2,

    /// <summary>Rule 3: a project in scope builds a container in a method without AddAutoMapper.</summary>
    ContainerWithoutRegistration = 3,

    /// <summary>The lookup's project is not part of the solution.</summary>
    NotInSolution = 4,
}
