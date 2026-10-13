using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core.Activators.Reflection;
using Autofac.Extensions.DependencyInjection;
using Autofac.Features.Variance;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace MyMeetings.Modules.Meetings.Infrastructure.Configuration.Mediation;

public interface IValidator<T>
{
}

public interface ICommand : IRequest
{
}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : global::MyMeetings.Modules.Meetings.Infrastructure.Configuration.Mediation.ICommand
{
}

public class AllConstructorFinder : IConstructorFinder
{
    public ConstructorInfo[] FindConstructors(Type targetType) => [.. targetType.GetTypeInfo().DeclaredConstructors.Where(c => !c.IsStatic)];
}

public class ServiceProviderWrapper : IServiceProvider
{
    private readonly ILifetimeScope _lifetimeScope;

    public ServiceProviderWrapper(ILifetimeScope lifetimeScope)
    {
        _lifetimeScope = lifetimeScope;
    }

    public object? GetService(Type serviceType) => _lifetimeScope.ResolveOptional(serviceType);
}

public class MediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterType<ServiceProviderWrapper>()
            .As<IServiceProvider>()
            .InstancePerDependency()
            .IfNotRegistered(typeof(IServiceProvider));

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
            typeof(IValidator<>),
            typeof(IRequestHandler<>),
            typeof(IStreamRequestHandler<,>),
            typeof(global::MyMeetings.Modules.Meetings.Infrastructure.Configuration.Mediation.ICommandHandler<>),
        };
        builder.RegisterSource(new ContravariantRegistrationSource());
        foreach (var mediatorOpenType in mediatorOpenTypes)
        {
            builder
                .RegisterAssemblyTypes(ThisAssembly)
                .AsClosedTypesOf(mediatorOpenType)
                .AsImplementedInterfaces()
                .FindConstructorsWith(new AllConstructorFinder());
        }
    }
}
