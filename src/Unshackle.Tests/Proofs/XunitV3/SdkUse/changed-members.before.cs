using System;
using Xunit.Sdk;

public static class TimeoutHelper
{
    public static Exception Timeout(int milliseconds) => new TestTimeoutException(milliseconds);

    public static RunSummary Empty() => new RunSummary();

    public static Exception Retry() => new TestTimeoutException(1000);
}
