namespace Unshackle.Core.Fixes.Moq;

// Why a Moq finding stays Manual (council round 8, general rule; round 10, C1: ManualResult.Code). The report prints "Code: reason".
public enum MoqReason
{
    ArgumentUsesMock,
    CallShape,
    ClassMock,
    ConstructorArguments,
    DefaultValueDiffers,
    MatcherNested,
    MatcherTypeDiffers,
    MatcherUnsupported,
    MockDeclaration,
    MockGet,
    MockOfSetup,
    MockOptions,
    MockPassedAround,
    MockUsedBeyondObject,
    NamedArgument,
    RefOutOrParams,
    ReturnsLambda,
    SetupShape,
    SetupsOverlap,
    StrictBehaviour,
    TargetFrameworkUnsupported,
    TimesRange,
    TooManyDefaultPins,
    VerifyMessage,
    VerifyNoOtherCalls,
    VerifyScopeNotLocal,
}
