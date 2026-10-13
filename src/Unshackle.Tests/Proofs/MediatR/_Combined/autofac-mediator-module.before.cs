// @project MyMeetings.Modules.Meetings.Application
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace MyMeetings.Modules.Meetings.Application;

public interface ICommand : IRequest
{
}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
{
}

public class AddMeetingCommentCommand : ICommand
{
}

internal class AddMeetingCommentCommandHandler : ICommandHandler<AddMeetingCommentCommand>
{
    internal AddMeetingCommentCommandHandler()
    {
    }

    public Task Handle(AddMeetingCommentCommand request, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class MeetingCommentAddedNotification : INotification
{
}

internal class MeetingCommentAddedNotificationHandler : INotificationHandler<MeetingCommentAddedNotification>
{
    public Task Handle(MeetingCommentAddedNotification notification, CancellationToken cancellationToken) => Task.CompletedTask;
}
// @project MyMeetings.Modules.Meetings.Infrastructure -> MyMeetings.Modules.Meetings.Application
using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core.Activators.Reflection;
using Autofac.Features.Variance;
using MediatR;
using MediatR.Pipeline;
using MyMeetings.Modules.Meetings.Application;

namespace MyMeetings.Modules.Meetings.Infrastructure.Configuration.Mediation;

public class AllConstructorFinder : IConstructorFinder
{
    public ConstructorInfo[] FindConstructors(Type targetType) => [.. targetType.GetTypeInfo().DeclaredConstructors.Where(c => !c.IsStatic)];
}

public class MediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        var mediatorOpenTypes = new[]
        {
            typeof(IRequestHandler<,>),
            typeof(INotificationHandler<>),
            typeof(IRequestPreProcessor<>),
            typeof(IRequestHandler<>),
            typeof(IStreamRequestHandler<,>),
            typeof(IRequestPostProcessor<,>),
            typeof(IRequestExceptionHandler<,,>),
            typeof(IRequestExceptionAction<,>),
            typeof(ICommandHandler<>),
        };
        builder.RegisterSource(new ContravariantRegistrationSource());
        foreach (var mediatorOpenType in mediatorOpenTypes)
        {
            builder
                .RegisterAssemblyTypes(typeof(ICommand).Assembly, ThisAssembly)
                .AsClosedTypesOf(mediatorOpenType)
                .AsImplementedInterfaces()
                .FindConstructorsWith(new AllConstructorFinder());
        }

        builder.RegisterGeneric(typeof(RequestPostProcessorBehavior<,>)).As(typeof(IPipelineBehavior<,>));
        builder.RegisterGeneric(typeof(RequestPreProcessorBehavior<,>)).As(typeof(IPipelineBehavior<,>));
    }
}
