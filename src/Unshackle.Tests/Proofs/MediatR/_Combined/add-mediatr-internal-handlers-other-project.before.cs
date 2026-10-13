// @project Clean.Architecture.Core
using System.Threading;
using System.Threading.Tasks;
using MediatR;

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
    public Task Handle(ContributorDeletedEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
}
// @project Clean.Architecture.UseCases -> Clean.Architecture.Core
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Clean.Architecture.UseCases.Contributors.Create;

public record CreateContributorCommand(string Name) : IRequest<int>;

public class CreateContributorHandler : IRequestHandler<CreateContributorCommand, int>
{
    public Task<int> Handle(CreateContributorCommand request, CancellationToken cancellationToken) => Task.FromResult(1);
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
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(mediatRAssemblies!));
    }
}
