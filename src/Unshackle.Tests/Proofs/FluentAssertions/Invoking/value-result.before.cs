using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ValueResultCase
{
    public async Task Test(Breaker breaker)
    {
        breaker.Invoking(b => b.Raise()).Should().Throw<InvalidOperationException>();
        await breaker.Awaiting(b => b.RaiseAsync()).Should().ThrowAsync<InvalidOperationException>();
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
