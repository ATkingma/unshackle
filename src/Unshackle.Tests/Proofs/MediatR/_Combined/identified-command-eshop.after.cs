using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public abstract class IdentifiedCommand<T, R> : IRequest<R>
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

public class IdentifiedCommandOfCancelOrderCommandAndBoolean : IdentifiedCommand<CancelOrderCommand, bool>
{
    public IdentifiedCommandOfCancelOrderCommandAndBoolean(CancelOrderCommand command, Guid id)
        : base(command, id)
    {
    }
}

public class IdentifiedCommandOfCreateOrderCommandAndBoolean : IdentifiedCommand<CreateOrderCommand, bool>
{
    public IdentifiedCommandOfCreateOrderCommandAndBoolean(CreateOrderCommand command, Guid id)
        : base(command, id)
    {
    }
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

    public async ValueTask<R> Handle(IdentifiedCommand<T, R> message, CancellationToken cancellationToken)
    {
        if (await _requestManager.ExistAsync(message.Id))
        {
            return CreateResultForDuplicateRequest();
        }

        return await _mediator.Send(message.Command, cancellationToken);
    }
}

public class CreateOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CreateOrderCommand, bool>, IRequestHandler<IdentifiedCommandOfCreateOrderCommandAndBoolean, bool>
{
    public CreateOrderIdentifiedCommandHandler(IMediator mediator, IRequestManager requestManager)
        : base(mediator, requestManager)
    {
    }

    protected override bool CreateResultForDuplicateRequest() => true;

    public ValueTask<bool> Handle(IdentifiedCommandOfCreateOrderCommandAndBoolean message, CancellationToken cancellationToken) => base.Handle(message, cancellationToken);
}

public class CancelOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CancelOrderCommand, bool>, IRequestHandler<IdentifiedCommandOfCancelOrderCommandAndBoolean, bool>
{
    public CancelOrderIdentifiedCommandHandler(IMediator mediator, IRequestManager requestManager)
        : base(mediator, requestManager)
    {
    }

    protected override bool CreateResultForDuplicateRequest() => true;

    public ValueTask<bool> Handle(IdentifiedCommandOfCancelOrderCommandAndBoolean message, CancellationToken cancellationToken) => base.Handle(message, cancellationToken);
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
        var identified = new IdentifiedCommandOfCreateOrderCommandAndBoolean(command, requestId);
        return _mediator.Send(identified, cancellationToken).AsTask();
    }

    public Task<bool> CancelOrderAsync(CancelOrderCommand command, Guid requestId, CancellationToken cancellationToken)
    {
        IdentifiedCommandOfCancelOrderCommandAndBoolean identified = new(command, requestId);
        return _mediator.Send(identified, cancellationToken).AsTask();
    }
}
