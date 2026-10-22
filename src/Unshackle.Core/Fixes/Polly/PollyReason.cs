namespace Unshackle.Core.Fixes.Polly;

// Why a Polly finding stays Manual (council round 8, general rule: a typed reason code with file:line). The code leads the
// reason text, so the report can be grouped on it until ManualResult gets a code field of its own.
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
}
