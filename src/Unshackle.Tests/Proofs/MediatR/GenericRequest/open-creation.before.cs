using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class Retried<TRequest, TResponse> : IRequest<TResponse>
    where TRequest : IRequest<TResponse>
{
    public Retried(TRequest request)
    {
        Request = request;
    }

    public TRequest Request { get; }
}

public class Client
{
    private readonly ISender _sender;

    public Client(ISender sender)
    {
        _sender = sender;
    }

    public Task<TResponse> SendRetriedAsync<TRequest, TResponse>(TRequest request)
        where TRequest : IRequest<TResponse> =>
        _sender.Send(new Retried<TRequest, TResponse>(request));
}