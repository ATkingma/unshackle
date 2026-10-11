using System.Threading;
using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public record Placed(int Id) : INotification;

public class AuditHandler : INotificationHandler<Placed>
{
    public ValueTask Handle(Placed notification, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

public class MailHandler : INotificationHandler<Placed>
{
    public async ValueTask Handle(Placed notification, CancellationToken cancellationToken) =>
        await Task.Yield();
}
