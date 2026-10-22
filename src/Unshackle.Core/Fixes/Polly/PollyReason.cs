namespace Unshackle.Core.Fixes.Polly;

// Why a Polly finding stays Manual (council round 8, general rule; round 10, C1: passed as ManualResult.Code, rendered "Code: reason").
public enum PollyReason
{
    PolicyNotLocal,
    ExceptionFilterNotSupported,
    OverloadNotSupported,
    RetryCountNotLiteral,
    DelayNotLambda,
    NameClash,
    PolicyUsedBeyondExecute,
    ExecuteOverloadNotSupported,
    ExecuteNotLambda,
    ResultNotAwaited,
    TargetFrameworkUnsupported,
    OnRetryUsesContext,
    OnRetryNotMappable,
    SleepDurationsNotLiteral,
    PolicyMemberNotPrivate,
}
