using System.Threading.Tasks;
using Mediator;

namespace Proofs;

public class Ping : IRequest<int>
{
}

public abstract class Retried<TRequest, TResponse> : IRequest<TResponse>
    where TRequest : IRequest<TResponse>
{
    public Retried(TRequest request, int attempts)
    {
        Request = request;
        Attempts = attempts;
    }

    public TRequest Request { get; }

    public int Attempts { get; }
}

public class RetriedOfPingAndInt32 : Retried<Ping, int>
{
    public RetriedOfPingAndInt32(Ping request, int attempts)
        : base(request, attempts)
    {
    }
}

public class Client
{
    private readonly ISender _sender;

    public Client(ISender sender)
    {
        _sender = sender;
    }

    public async Task<int> SendAsync(Ping ping)
    {
        RetriedOfPingAndInt32 retried = new(ping, 3);
        return await _sender.Send(retried);
    }

    public async Task<int> SendOnceAsync(Ping ping) => await _sender.Send(new RetriedOfPingAndInt32(ping, 1));
}