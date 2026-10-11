namespace Proofs.Mediator
{
    public static class MediatorModule
    {
    }
}

namespace Proofs.Events
{
    using global::Mediator;

    public record OrderPlaced(int Id) : INotification;
}
