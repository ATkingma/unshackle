using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class Shop
{
    public async Task PlaceAsync(IPublisher publisher) => await publisher.Publish(new PlacedOfInt32(1));
}

public abstract class Placed<T> : INotification
{
    public Placed(T id)
    {
        Id = id;
    }

    public T Id { get; }
}

public class PlacedOfInt32 : Placed<int>
{
    public PlacedOfInt32(int id)
        : base(id)
    {
    }
}