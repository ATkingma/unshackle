using System.Reflection;
using Autofac;
using MediatR;

namespace Shop.Infrastructure;

public class MediatorModule : Autofac.Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(typeof(IMediator).Assembly).AsImplementedInterfaces().PropertiesAutowired();
    }
}
