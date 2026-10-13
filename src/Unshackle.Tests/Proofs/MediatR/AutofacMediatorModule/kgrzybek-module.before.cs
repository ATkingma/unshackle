using System;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Core.Activators.Reflection;
using Autofac.Features.Variance;
using MediatR;
using MediatR.Pipeline;

namespace MyMeetings.Modules.Meetings.Infrastructure.Configuration.Mediation;

public interface IValidator<T>
{
}

public interface ICommand : IRequest
{
}

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand>
    where TCommand : ICommand
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

        builder.RegisterAssemblyTypes(typeof(IMediator).GetTypeInfo().Assembly)
            .AsImplementedInterfaces()
            .InstancePerLifetimeScope();

        var mediatorOpenTypes = new[]
        {
            typeof(IRequestHandler<,>),
            typeof(INotificationHandler<>),
            typeof(IValidator<>),
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
                .RegisterAssemblyTypes(ThisAssembly)
                .AsClosedTypesOf(mediatorOpenType)
                .AsImplementedInterfaces()
                .FindConstructorsWith(new AllConstructorFinder());
        }

        builder.RegisterGeneric(typeof(RequestPostProcessorBehavior<,>)).As(typeof(IPipelineBehavior<,>));
        builder.RegisterGeneric(typeof(RequestPreProcessorBehavior<,>)).As(typeof(IPipelineBehavior<,>));
    }
}
