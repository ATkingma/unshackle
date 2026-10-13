using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
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
        _mediator.Send(Arg.Any<IRequest<bool>>(), default).Returns(Task.FromResult(true));
        _mediator.Send(Arg.Any<Query>(), default).ReturnsForAnyArgs(Task.FromResult<bool>(false), System.Threading.Tasks.Task.FromResult(true));
        _mediator.Publish(Arg.Any<Placed>(), default).Returns(Task.CompletedTask);
        _mediator.Send(new Query(2), default).Throws(new InvalidOperationException());
        _mediator.Received().Send(new Query(3), default);

        await _mediator.Received().Send(Arg.Any<Query>(), default);
    }
}
