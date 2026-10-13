// @project MyMeetings.Modules.Meetings.Application
using System.Threading;
using System.Threading.Tasks;
using Mediator;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyMeetings.Modules.Meetings.Infrastructure")]

namespace MyMeetings.Modules.Meetings.Application;

public interface ICommand : IRequest
{
}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : global::MyMeetings.Modules.Meetings.Application.ICommand
{
}

public class AddMeetingCommentCommand : global::MyMeetings.Modules.Meetings.Application.ICommand
{
}

internal class AddMeetingCommentCommandHandler : global::MyMeetings.Modules.Meetings.Application.ICommandHandler<AddMeetingCommentCommand>
{
    internal AddMeetingCommentCommandHandler()
    {
    }

    public ValueTask<Unit> Handle(AddMeetingCommentCommand request, CancellationToken cancellationToken) => Unit.ValueTask;
}

public class MeetingCommentAddedNotification : INotification
{
}

internal class MeetingCommentAddedNotificationHandler : INotificationHandler<MeetingCommentAddedNotification>
{
    public ValueTask Handle(MeetingCommentAddedNotification notification, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
// @project MyMeetings.Modules.Meetings.Infrastructure -> MyMeetings.Modules.Meetings.Application
using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core.Activators.Reflection;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.Variance;
using Mediator;
using MyMeetings.Modules.Meetings.Application;
using Microsoft.Extensions.DependencyInjection;

namespace MyMeetings.Modules.Meetings.Infrastructure.Configuration.Mediation;

public class AllConstructorFinder : IConstructorFinder
{
    public ConstructorInfo[] FindConstructors(Type targetType) => [.. targetType.GetTypeInfo().DeclaredConstructors.Where(c => !c.IsStatic)];
}

public class MediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Mediator's dispatcher; the handlers stay registered by this module, as they were for MediatR.
        var mediatorServices = new ServiceCollection();
        mediatorServices.AddMediator(mediatorOptions =>
        {
            mediatorOptions.ServiceLifetime = ServiceLifetime.Scoped;
            mediatorOptions.NotificationPublisherType = typeof(global::Mediator.TaskWhenAllPublisher);
        });
        builder.Populate(mediatorServices.Where(descriptor => descriptor.ServiceType.Namespace is "Mediator" or "Mediator.Internals"
            && !(descriptor.ServiceType.IsGenericType && descriptor.ServiceType.Assembly == typeof(global::Mediator.IMediator).Assembly)));

        var mediatorOpenTypes = new[]
        {
            typeof(IRequestHandler<,>),
            typeof(INotificationHandler<>),
            typeof(IRequestHandler<>),
            typeof(IStreamRequestHandler<,>),
            typeof(global::MyMeetings.Modules.Meetings.Application.ICommandHandler<>),
        };
        builder.RegisterSource(new ContravariantRegistrationSource());
        foreach (var mediatorOpenType in mediatorOpenTypes)
        {
            builder
                .RegisterAssemblyTypes(typeof(global::MyMeetings.Modules.Meetings.Application.ICommand).Assembly, ThisAssembly)
                .AsClosedTypesOf(mediatorOpenType)
                .AsImplementedInterfaces()
                .FindConstructorsWith(new AllConstructorFinder());
        }
    }
}
