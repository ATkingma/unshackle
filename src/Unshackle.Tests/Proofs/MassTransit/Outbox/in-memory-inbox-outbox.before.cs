using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

public static class Bus
{
    public static void Register(IServiceCollection services) =>
        services.AddMassTransit(x => x.AddInMemoryInboxOutbox());
}
