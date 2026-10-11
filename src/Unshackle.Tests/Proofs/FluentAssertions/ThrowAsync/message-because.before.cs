using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class MessageBecauseCase
{
    public async Task Test(Func<Task> act)
    {
        await act.Should().ThrowAsync<ArgumentException>("a").WithMessage("*shipped*", "b");
    }
}
