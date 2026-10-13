// @project App
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace App;

public interface ICommand : IRequest
{
}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}

public class CreateMeetingCommand : ICommand
{
}

public class CreateMeetingCommandHandler : ICommandHandler<CreateMeetingCommand>
{
    public Task Handle(CreateMeetingCommand command, CancellationToken cancellationToken) => Task.CompletedTask;
}
// @project Handlers -> App
using System.Threading;
using System.Threading.Tasks;
using App;

namespace Handlers;

internal class CancelMeetingCommand : ICommand
{
}

internal class CancelMeetingCommandHandler : ICommandHandler<CancelMeetingCommand>
{
    public async Task Handle(CancelMeetingCommand command, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
    }
}
// @project Infra -> App
using System;
using System.Threading;
using System.Threading.Tasks;
using App;
using Autofac;

namespace Infra;

internal class LoggingCommandHandlerDecorator<T> : ICommandHandler<T>
    where T : ICommand
{
    private readonly ICommandHandler<T> _decorated;

    public LoggingCommandHandlerDecorator(ICommandHandler<T> decorated)
    {
        _decorated = decorated;
    }

    public async Task Handle(T command, CancellationToken cancellationToken)
    {
        Console.WriteLine("Executing command " + command.GetType().Name);
        await _decorated.Handle(command, cancellationToken);
    }
}

internal class ProcessingModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterGenericDecorator(
            typeof(LoggingCommandHandlerDecorator<>),
            typeof(ICommandHandler<>));
    }
}
