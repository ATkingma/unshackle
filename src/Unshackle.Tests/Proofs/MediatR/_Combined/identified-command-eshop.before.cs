using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class IdentifiedCommand<T, R> : IRequest<R>
    where T : IRequest<R>
{
    public IdentifiedCommand(T command, Guid id)
    {
        Command = command;
        Id = id;
    }

    public T Command { get; }

    public Guid Id { get; }
}

public class CreateOrderCommand : IRequest<bool>
{
}

public class CancelOrderCommand : IRequest<bool>
{
}

public interface IRequestManager
{
    Task<bool> ExistAsync(Guid id);
}

public abstract class IdentifiedCommandHandler<T, R> : IRequestHandler<IdentifiedCommand<T, R>, R>
    where T : IRequest<R>
{
    private readonly IMediator _mediator;
    private readonly IRequestManager _requestManager;

    protected IdentifiedCommandHandler(IMediator mediator, IRequestManager requestManager)
    {
        _mediator = mediator;
        _requestManager = requestManager;
    }

    protected abstract R CreateResultForDuplicateRequest();

    public async Task<R> Handle(IdentifiedCommand<T, R> message, CancellationToken cancellationToken)
    {
        if (await _requestManager.ExistAsync(message.Id))
        {
            return CreateResultForDuplicateRequest();
        }

        return await _mediator.Send(message.Command, cancellationToken);
    }
}

public class CreateOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CreateOrderCommand, bool>
{
    public CreateOrderIdentifiedCommandHandler(IMediator mediator, IRequestManager requestManager)
        : base(mediator, requestManager)
    {
    }

    protected override bool CreateResultForDuplicateRequest() => true;
}

public class CancelOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CancelOrderCommand, bool>
{
    public CancelOrderIdentifiedCommandHandler(IMediator mediator, IRequestManager requestManager)
        : base(mediator, requestManager)
    {
    }

    protected override bool CreateResultForDuplicateRequest() => true;
}

public class OrdersApi
{
    private readonly IMediator _mediator;

    public OrdersApi(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Task<bool> CreateOrderAsync(CreateOrderCommand command, Guid requestId, CancellationToken cancellationToken)
    {
        var identified = new IdentifiedCommand<CreateOrderCommand, bool>(command, requestId);
        return _mediator.Send(identified, cancellationToken);
    }

    public Task<bool> CancelOrderAsync(CancelOrderCommand command, Guid requestId, CancellationToken cancellationToken)
    {
        IdentifiedCommand<CancelOrderCommand, bool> identified = new(command, requestId);
        return _mediator.Send(identified, cancellationToken);
    }
}
