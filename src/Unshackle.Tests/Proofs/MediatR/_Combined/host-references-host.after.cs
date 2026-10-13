// @project MyMeetings.Modules.UserAccess.Infrastructure
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace MyMeetings.Modules.UserAccess.Infrastructure;

public class ProcessInboxCommand : IRequest
{
}

internal class ProcessInboxCommandHandler : IRequestHandler<ProcessInboxCommand>
{
    public ValueTask<Unit> Handle(ProcessInboxCommand request, CancellationToken cancellationToken) => Unit.ValueTask;
}

public class UserAccessMediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Mediator's dispatcher; the handlers stay registered by this module, as they were for MediatR.
        var mediatorServices = new ServiceCollection();
        mediatorServices.AddMediator(mediatorOptions =>
        {
            mediatorOptions.ServiceLifetime = ServiceLifetime.Scoped;
            mediatorOptions.NotificationPublisherType = typeof(global::Mediator.TaskWhenAllPublisher);
            mediatorOptions.GenerateTypesAsInternal = true;
        });
        builder.Populate(mediatorServices.Where(descriptor => descriptor.ServiceType.Namespace is "Mediator" or "Mediator.Internals"
            && !(descriptor.ServiceType.IsGenericType && descriptor.ServiceType.Assembly == typeof(global::Mediator.IMediator).Assembly)));
        builder.RegisterAssemblyTypes(ThisAssembly)
            .AsClosedTypesOf(typeof(IRequestHandler<>))
            .AsImplementedInterfaces();
    }
}
// @project MyMeetings.Modules.Registrations.Application
using System.Threading;
using System.Threading.Tasks;
using Mediator;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MyMeetings.Modules.Registrations.Infrastructure")]

namespace MyMeetings.Modules.Registrations.Application;

public class ConfirmUserRegistrationCommand : IRequest
{
}

internal class ConfirmUserRegistrationCommandHandler : IRequestHandler<ConfirmUserRegistrationCommand>
{
    public ValueTask<Unit> Handle(ConfirmUserRegistrationCommand request, CancellationToken cancellationToken) => Unit.ValueTask;
}
// @project MyMeetings.Modules.Registrations.Infrastructure -> MyMeetings.Modules.Registrations.Application, MyMeetings.Modules.UserAccess.Infrastructure
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using MyMeetings.Modules.Registrations.Application;

namespace MyMeetings.Modules.Registrations.Infrastructure;

public class RegistrationsMediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Mediator's dispatcher; the handlers stay registered by this module, as they were for MediatR.
        var mediatorServices = new ServiceCollection();
        mediatorServices.AddMediator(mediatorOptions =>
        {
            mediatorOptions.ServiceLifetime = ServiceLifetime.Scoped;
            mediatorOptions.NotificationPublisherType = typeof(global::Mediator.TaskWhenAllPublisher);
            mediatorOptions.Assemblies = [typeof(global::MyMeetings.Modules.Registrations.Infrastructure.RegistrationsMediatorModule), typeof(global::MyMeetings.Modules.Registrations.Application.ConfirmUserRegistrationCommand)];
        });
        builder.Populate(mediatorServices.Where(descriptor => descriptor.ServiceType.Namespace is "Mediator" or "Mediator.Internals"
            && !(descriptor.ServiceType.IsGenericType && descriptor.ServiceType.Assembly == typeof(global::Mediator.IMediator).Assembly)));
        builder.RegisterAssemblyTypes(typeof(ConfirmUserRegistrationCommand).Assembly, ThisAssembly)
            .AsClosedTypesOf(typeof(IRequestHandler<>))
            .AsImplementedInterfaces();
    }
}
