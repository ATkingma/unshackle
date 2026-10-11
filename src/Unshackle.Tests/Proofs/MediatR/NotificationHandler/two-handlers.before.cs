using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Placed(int Id) : INotification;

public class AuditHandler : INotificationHandler<Placed>
{
    public Task Handle(Placed notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public class MailHandler : INotificationHandler<Placed>
{
    public async Task Handle(Placed notification, CancellationToken cancellationToken) =>
        await Task.Yield();
}
