namespace Proofs.Mediator
{
    public static class MediatorModule
    {
    }
}

namespace Proofs.Events
{
    using MediatR;

    public record OrderPlaced(int Id) : INotification;
}
