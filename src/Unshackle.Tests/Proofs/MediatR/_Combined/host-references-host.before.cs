// @project MyMeetings.Modules.UserAccess.Infrastructure
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MediatR;

namespace MyMeetings.Modules.UserAccess.Infrastructure;

public class ProcessInboxCommand : IRequest
{
}

internal class ProcessInboxCommandHandler : IRequestHandler<ProcessInboxCommand>
{
    public Task Handle(ProcessInboxCommand request, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class UserAccessMediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(IRequestHandler<>))
            .AsImplementedInterfaces();
    }
}
// @project MyMeetings.Modules.Registrations.Application
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace MyMeetings.Modules.Registrations.Application;

public class ConfirmUserRegistrationCommand : IRequest
{
}

internal class ConfirmUserRegistrationCommandHandler : IRequestHandler<ConfirmUserRegistrationCommand>
{
    public Task Handle(ConfirmUserRegistrationCommand request, CancellationToken cancellationToken) => Task.CompletedTask;
}
// @project MyMeetings.Modules.Registrations.Infrastructure -> MyMeetings.Modules.Registrations.Application, MyMeetings.Modules.UserAccess.Infrastructure
using System.Reflection;
using Autofac;
using MediatR;
using MyMeetings.Modules.Registrations.Application;

namespace MyMeetings.Modules.Registrations.Infrastructure;

public class RegistrationsMediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();
        builder.RegisterAssemblyTypes(typeof(ConfirmUserRegistrationCommand).Assembly, ThisAssembly)
            .AsClosedTypesOf(typeof(IRequestHandler<>))
            .AsImplementedInterfaces();
    }
}
