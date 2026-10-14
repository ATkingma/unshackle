using System;
using System.Threading;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

// Shape from Steeltoe (Apache-2.0): HealthContributorTest.Canceled_Throws without Exactly or a step (TAAK-221).
public class CancelledPlainCase
{
    public async Task Test(Func<CancellationToken, Task> check)
    {
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        Func<Task> action = async () => await check(source.Token);
        await Should.ThrowAsync<OperationCanceledException>(action);
    }
}
