using System;
using System.Threading.Tasks;
using FluentAssertions;

namespace Proofs;

public class ResultLocalMessageInnerCase
{
    public async Task Test(Func<Task> act)
    {
        var ex = await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("The circuit is now open and is not allowing calls.");
        ex.WithInnerException<ArgumentException>();
    }
}
