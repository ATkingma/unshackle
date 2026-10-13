using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class Wrapped<T> : INotification
{
    public Wrapped(T value)
    {
        Value = value;
    }

    public T Value { get; }
}

public class Relay
{
    private readonly IPublisher _publisher;

    public Relay(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public Task RelayAsync<TValue>(TValue value) => _publisher.Publish(new Wrapped<TValue>(value));
}