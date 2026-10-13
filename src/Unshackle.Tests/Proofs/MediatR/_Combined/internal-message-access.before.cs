// @project MyMeetings.Modules.Meetings.Domain
using MediatR;

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
using MediatR;

namespace MyMeetings.Modules.Meetings.Application;

public class CancelMeetingCommand : IRequest
{
}

public class CancelMeetingCommandHandler : IRequestHandler<CancelMeetingCommand>
{
    public Task Handle(CancelMeetingCommand request, CancellationToken cancellationToken) => Task.CompletedTask;
}
// @project MyMeetings.Modules.Meetings.Infrastructure -> MyMeetings.Modules.Meetings.Application, MyMeetings.Modules.Meetings.Domain
using System.Reflection;
using Autofac;
using MediatR;
using MyMeetings.Modules.Meetings.Application;

namespace MyMeetings.Modules.Meetings.Infrastructure.Configuration.Mediation;

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
