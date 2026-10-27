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

internal static class RetryExtensions
{
    public static Task<TResponse> SendRetriedAsync<TRequest, TResponse>(this ISender sender, TRequest request)
        where TRequest : IRequest<TResponse> => sender.Send(new Retried<TRequest, TResponse>(request, 3));
}

public class Client
{
    private readonly ISender _sender;

    public Client(ISender sender)
    {
        _sender = sender;
    }

    public async Task<int> SendAsync(Ping ping) => await _sender.SendRetriedAsync<Ping, int>(ping);
}
