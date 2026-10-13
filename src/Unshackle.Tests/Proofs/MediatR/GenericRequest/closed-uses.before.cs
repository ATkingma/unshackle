using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class Ping : IRequest<int>
{
}

public class Retried<TRequest, TResponse> : IRequest<TResponse>
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

public class Client
{
    private readonly ISender _sender;

    public Client(ISender sender)
    {
        _sender = sender;
    }

    public async Task<int> SendAsync(Ping ping)
    {
        Retried<Ping, int> retried = new(ping, 3);
        return await _sender.Send(retried);
    }

    public async Task<int> SendOnceAsync(Ping ping) => await _sender.Send(new Retried<Ping, int>(ping, 1));
}