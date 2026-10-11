using System;
using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Query(int Id) : IRequest<int>;

public record Placed(int Id) : INotification;

public class Facade
{
    private readonly ISender _sender;
    private readonly IPublisher _publisher;

    public Facade(ISender sender, IPublisher publisher)
    {
        _sender = sender;
        _publisher = publisher;
    }

    public Task<int> GetAsync(int id, CancellationToken cancellationToken) => _sender.Send(new Query(id), cancellationToken).AsTask();

    public async Task<int> AllAsync(CancellationToken cancellationToken)
    {
        Func<Task> act = () => _publisher.Publish(new Placed(1), cancellationToken).AsTask();
        await act();
        int[] both = await Task.WhenAll(_sender.Send(new Query(1), cancellationToken).AsTask(), _sender.Send(new Query(2), cancellationToken).AsTask());
        int next = await _sender.Send(new Query(3), cancellationToken).AsTask().ContinueWith(t => t.Result + 1, cancellationToken);
        return both[0] + next + await _sender.Send(new Query(4), cancellationToken) + await _sender.Send(new Query(5), cancellationToken).ConfigureAwait(false);
    }
}
