namespace Unshackle.Core.Models;

/// <summary>
/// Why a finding stays on the old library while the rest of the fix goes ahead (council round 3, Q2). A <see cref="ManualResult"/> with a
/// kept reason does not undo the fix; the report renderer prints the prefix of its fix in front of its reason (<see cref="KeptReasonText"/>).
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

    /// <summary>The result of AddAutoMapper is used (not a statement on its own), so the registration stays.</summary>
    RegistrationResultUsed = 5,

    /// <summary>IMapper is still used, so AddAutoMapper stays.</summary>
    MapperStillUsed = 6,

    /// <summary>Maps of the solution stay on AutoMapper, so a configuration check or test mapper stays.</summary>
    ConfigurationCheckWithKeptMaps = 7,

    /// <summary>AssertConfigurationIsValid still has a map to check that is not in a profile constructor this fix rewrites.</summary>
    ConfigurationCheckWithUnmigratedMap = 8,

    /// <summary>Every map of a profile stays on AutoMapper.</summary>
    ProfileKept = 9,

    /// <summary>One CreateMap stays in a profile whose other maps are migrated.</summary>
    MapKept = 10,

    /// <summary>A mock set-up of IMapper in a test stays for the class under test.</summary>
    MockSetUpKept = 11,

    /// <summary>A Map, ProjectTo or similar call keeps using IMapper because its map stays on AutoMapper.</summary>
    CallKept = 12,

    /// <summary>MediatR: code tied to a finding that cannot be migrated stays on MediatR next to Mediator (partial migration, TAAK-274).</summary>
    MediatRNextToMediator = 13,
}
