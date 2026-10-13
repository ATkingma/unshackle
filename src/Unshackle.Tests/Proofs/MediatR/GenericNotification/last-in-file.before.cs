using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class Shop
{
    public async Task PlaceAsync(IPublisher publisher) => await publisher.Publish(new Placed<int>(1));
}

public class Placed<T> : INotification
{
    public Placed(T id)
    {
        Id = id;
    }

    public T Id { get; }
}