using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Proofs;

public record Query(int Id) : IRequest<bool>;

public record Placed(int Id) : INotification;

public class OrdersWebApiTest
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    public async Task Setups()
    {
        _mediator.Send(Arg.Any<IRequest<bool>>(), default).Returns(ValueTask.FromResult(true));
        _mediator.Send(Arg.Any<Query>(), default).ReturnsForAnyArgs(ValueTask.FromResult<bool>(false), System.Threading.Tasks.ValueTask.FromResult(true));
        _mediator.Publish(Arg.Any<Placed>(), default).Returns(ValueTask.CompletedTask);
        _mediator.Send(new Query(2), default).Throws(new InvalidOperationException());
        _mediator.Received().Send(new Query(3), default);

        await _mediator.Received().Send(Arg.Any<Query>(), default);
    }
}
