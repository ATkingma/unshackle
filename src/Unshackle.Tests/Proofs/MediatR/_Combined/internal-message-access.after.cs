// @project MyMeetings.Modules.Meetings.Domain
using Mediator;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyMeetings.Modules.Meetings.Infrastructure")]

namespace MyMeetings.Modules.Meetings.Domain;

public interface IDomainEvent : INotification
{
}

internal class MeetingCanceledDomainEvent : IDomainEvent
{
}
// @project MyMeetings.Modules.Meetings.Application -> MyMeetings.Modules.Meetings.Domain
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace MyMeetings.Modules.Meetings.Application;

public class CancelMeetingCommand : IRequest
{
}

public class CancelMeetingCommandHandler : IRequestHandler<CancelMeetingCommand>
{
    public ValueTask<Unit> Handle(CancelMeetingCommand request, CancellationToken cancellationToken) => Unit.ValueTask;
}
// @project MyMeetings.Modules.Meetings.Infrastructure -> MyMeetings.Modules.Meetings.Application, MyMeetings.Modules.Meetings.Domain
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyMeetings.Modules.Meetings.Application;

namespace MyMeetings.Modules.Meetings.Infrastructure.Configuration.Mediation;

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
        };
        foreach (var mediatorOpenType in mediatorOpenTypes)
        {
            builder
                .RegisterAssemblyTypes(typeof(CancelMeetingCommand).Assembly, ThisAssembly)
                .AsClosedTypesOf(mediatorOpenType)
                .AsImplementedInterfaces();
        }
    }
}
