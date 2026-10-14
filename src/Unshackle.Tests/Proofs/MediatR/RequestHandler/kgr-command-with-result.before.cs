using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

// Shape from kgrzybek/modular-monolith-with-ddd (MIT): own ICommand<TResult> and ICommandHandler markers over MediatR.
public interface ICommand<out TResult> : IRequest<TResult>
{
    Guid Id { get; }
}

public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
}

public class CreateMeetingCommand : ICommand<Guid>
{
    public Guid Id { get; } = Guid.NewGuid();
}

internal class CreateMeetingCommandHandler : ICommandHandler<CreateMeetingCommand, Guid>
{
    public async Task<Guid> Handle(CreateMeetingCommand command, CancellationToken cancellationToken)
    {
        await Task.Yield();
        return command.Id;
    }
}
