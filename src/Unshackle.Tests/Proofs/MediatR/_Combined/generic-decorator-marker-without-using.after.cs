// @project App
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace App;

public interface ICommand : IRequest
{
}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : global::App.ICommand
{
}

public class CreateMeetingCommand : global::App.ICommand
{
}

public class CreateMeetingCommandHandler : global::App.ICommandHandler<CreateMeetingCommand>
{
    public ValueTask<Unit> Handle(CreateMeetingCommand command, CancellationToken cancellationToken) => Unit.ValueTask;
}
// @project Handlers -> App
using System.Threading;
using System.Threading.Tasks;
using App;
using Mediator;

namespace Handlers;

internal class CancelMeetingCommand : global::App.ICommand
{
}

internal class CancelMeetingCommandHandler : global::App.ICommandHandler<CancelMeetingCommand>
{
    public async ValueTask<Unit> Handle(CancelMeetingCommand command, CancellationToken cancellationToken)
    {
        await Task.Delay(1, cancellationToken);
        return Unit.Value;
    }
}
// @project Infra -> App
using System;
using System.Threading;
using System.Threading.Tasks;
using App;
using Autofac;
using Mediator;

namespace Infra;

internal class LoggingCommandHandlerDecorator<T> : global::App.ICommandHandler<T>
    where T : global::App.ICommand
{
    private readonly IRequestHandler<T, Unit> _decorated;

    public LoggingCommandHandlerDecorator(IRequestHandler<T, Unit> decorated)
    {
        _decorated = decorated;
    }

    public async ValueTask<Unit> Handle(T command, CancellationToken cancellationToken)
    {
        Console.WriteLine("Executing command " + command.GetType().Name);
        await _decorated.Handle(command, cancellationToken);
        return Unit.Value;
    }
}

internal class ProcessingModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterGenericDecorator(
            typeof(LoggingCommandHandlerDecorator<>),
            typeof(IRequestHandler<,>));
    }
}
