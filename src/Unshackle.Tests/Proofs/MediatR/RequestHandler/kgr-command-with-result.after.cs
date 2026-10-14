using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

// Shape from kgrzybek/modular-monolith-with-ddd (MIT): own ICommand<TResult> and ICommandHandler markers over MediatR.
public interface ICommand<out TResult> : IRequest<TResult>
{
    Guid Id { get; }
}

public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : global::Proofs.ICommand<TResult>
{
}

public class CreateMeetingCommand : global::Proofs.ICommand<Guid>
{
    public Guid Id { get; } = Guid.NewGuid();
}

internal class CreateMeetingCommandHandler : global::Proofs.ICommandHandler<CreateMeetingCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateMeetingCommand command, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return command.Id;
    }
}
