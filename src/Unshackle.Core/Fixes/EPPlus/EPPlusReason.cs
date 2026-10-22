namespace Unshackle.Core.Fixes.EPPlus;

// Why an EPPlus finding stays Manual (council round 10, C1): the typed reason code the report prints in front of the reason.
public enum EPPlusReason
{
    UnmigratedEPPlusApi,
    PublicApiExposesOldLibrary,
    LicensedDependency,
    UnknownEPPlusVersion,
    WorksheetIndexBaseNotProvable,
    UnmappedEPPlusStyle,
    FillWithoutSolidPattern,
    UncoveredRuntimeType,
    WorksheetIndexConfigured,
    CellReadDiffers,
}
