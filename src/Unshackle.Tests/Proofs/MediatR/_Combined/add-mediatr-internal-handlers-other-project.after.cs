// @project Clean.Architecture.Core
using System.Threading;
using System.Threading.Tasks;
using Mediator;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Clean.Architecture.Web")]

namespace Clean.Architecture.Core.ContributorAggregate;

public class Contributor
{
    public int Id { get; set; }
}

internal class ContributorDeletedEvent : INotification
{
    public int ContributorId { get; init; }
}

internal class ContributorDeletedHandler : INotificationHandler<ContributorDeletedEvent>
{
    public ValueTask Handle(ContributorDeletedEvent domainEvent, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
// @project Clean.Architecture.UseCases -> Clean.Architecture.Core
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Clean.Architecture.UseCases.Contributors.Create;

public record CreateContributorCommand(string Name) : IRequest<int>;

public class CreateContributorHandler : IRequestHandler<CreateContributorCommand, int>
{
    public ValueTask<int> Handle(CreateContributorCommand request, CancellationToken cancellationToken) => ValueTask.FromResult(1);
}
// @project Clean.Architecture.Web -> Clean.Architecture.Core, Clean.Architecture.UseCases
using System.Reflection;
using Clean.Architecture.Core.ContributorAggregate;
using Clean.Architecture.UseCases.Contributors.Create;
using Microsoft.Extensions.DependencyInjection;

namespace Clean.Architecture.Web;

public static class Startup
{
    public static void ConfigureMediatR(IServiceCollection services)
    {
        var mediatRAssemblies = new[]
        {
            Assembly.GetAssembly(typeof(Contributor)), // Core
            Assembly.GetAssembly(typeof(CreateContributorCommand)) // UseCases
        };
        services.AddMediator(cfg => cfg.ServiceLifetime = ServiceLifetime.Transient);
    }
}
