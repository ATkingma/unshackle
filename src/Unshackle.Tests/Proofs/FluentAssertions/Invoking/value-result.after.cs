using System;
using System.Threading.Tasks;
using Shouldly;

namespace Proofs;

public class ValueResultCase
{
    public async Task Test(Breaker breaker)
    {
        Should.Throw<InvalidOperationException>(() => breaker.Raise());
        await Should.ThrowAsync<InvalidOperationException>(() => breaker.RaiseAsync());
    }
}

public enum State
{
    Closed,
    Open,
}

public class Breaker
{
    public State Raise() => throw new InvalidOperationException();

    public Task<int> RaiseAsync() => Task.FromException<int>(new InvalidOperationException());

    public Task<int> CountAsync() => Task.FromResult(1);
}
