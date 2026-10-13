using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.SharedKernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Proofs;

public class ContributorDeletedEvent(int contributorId) : DomainEventBase
{
    public int ContributorId { get; } = contributorId;
}

public class Contributor : EntityBase, IAggregateRoot
{
    public PhoneNumber? Phone { get; set; }

    public void Delete() => RegisterDomainEvent(new ContributorDeletedEvent(Id));
}

public class PhoneNumber(string number) : ValueObject
{
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return number;
    }
}

public class ContributorDeletedHandler : INotificationHandler<ContributorDeletedEvent>
{
    public Task Handle(ContributorDeletedEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
}

public record DeleteContributorCommand(int ContributorId) : ICommand<bool>;

public class DeleteContributorHandler(IRepository<Contributor> repository) : ICommandHandler<DeleteContributorCommand, bool>
{
    public async Task<bool> Handle(DeleteContributorCommand request, CancellationToken cancellationToken)
    {
        Contributor? contributor = await repository.GetByIdAsync(request.ContributorId, cancellationToken);
        if (contributor is null)
        {
            return false;
        }

        contributor.Delete();
        await repository.DeleteAsync(contributor, cancellationToken);
        return true;
    }
}

public record ContributorNameQuery(int ContributorId) : IQuery<string?>;

public class ContributorNameHandler(IReadRepository<Contributor> repository) : IQueryHandler<ContributorNameQuery, string?>
{
    public async Task<string?> Handle(ContributorNameQuery request, CancellationToken cancellationToken) =>
        (await repository.GetByIdAsync(request.ContributorId, cancellationToken))?.Id.ToString();
}

public class Dispatch(IDomainEventDispatcher dispatcher)
{
    public Task SaveAsync(IEnumerable<EntityBase> entities) => dispatcher.DispatchAndClearEvents(entities);
}

public static class Registration
{
    public static IServiceCollection AddKernel(this IServiceCollection services)
    {
        services.AddMediatR(c => c.RegisterServicesFromAssemblyContaining<Contributor>());
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        return services;
    }
}
