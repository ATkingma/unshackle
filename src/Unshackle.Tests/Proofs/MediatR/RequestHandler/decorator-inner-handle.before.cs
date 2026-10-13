using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public class ValidationDecorator<TRequest, TResult> : IRequestHandler<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    private readonly IRequestHandler<TRequest, TResult> _decorated;

    public ValidationDecorator(IRequestHandler<TRequest, TResult> decorated)
    {
        _decorated = decorated;
    }

    public Task<TResult> Handle(TRequest request, CancellationToken cancellationToken)
    {
        return _decorated.Handle(request, cancellationToken);
    }
}
