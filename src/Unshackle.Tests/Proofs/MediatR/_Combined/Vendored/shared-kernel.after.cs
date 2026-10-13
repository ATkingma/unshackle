using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.SharedKernel;
using Mediator;
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
    public ValueTask Handle(ContributorDeletedEvent domainEvent, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

public record DeleteContributorCommand(int ContributorId) : global::Ardalis.SharedKernel.ICommand<bool>;

public class DeleteContributorHandler(IRepository<Contributor> repository) : global::Ardalis.SharedKernel.ICommandHandler<DeleteContributorCommand, bool>
{
    public async ValueTask<bool> Handle(DeleteContributorCommand request, CancellationToken cancellationToken)
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

public record ContributorNameQuery(int ContributorId) : global::Ardalis.SharedKernel.IQuery<string?>;

public class ContributorNameHandler(IReadRepository<Contributor> repository) : global::Ardalis.SharedKernel.IQueryHandler<ContributorNameQuery, string?>
{
    public async ValueTask<string?> Handle(ContributorNameQuery request, CancellationToken cancellationToken) =>
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
        services.AddMediator(c => c.ServiceLifetime = ServiceLifetime.Transient);
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
        return services;
    }
}
