using System.Threading;
using System.Threading.Tasks;

namespace Proofs.Application
{
    public interface ICommand<out TResult>
    {
        TResult Value { get; }
    }
}

namespace Proofs.Infrastructure
{
    using Application;
    using Mediator;

    public record Ping(int Id) : IRequest<int>;

    public class PingHandler : IRequestHandler<Ping, int>
    {
        public async ValueTask<int> Handle(Ping request, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
            return request.Id;
        }
    }

    public class Module(ISender sender)
    {
        public async Task<int> ExecuteAsync(global::Proofs.Application.ICommand<int> command) => await sender.Send(new Ping(command.Value));
    }
}
