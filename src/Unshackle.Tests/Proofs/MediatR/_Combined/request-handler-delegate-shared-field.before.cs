using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;

namespace Proofs;

public record Response(string Value);

public interface IAuthorizeableRequest<T> : IRequest<T>;

public record PlainRequest : IAuthorizeableRequest<Response>;

public record SecuredRequest(string Role) : IAuthorizeableRequest<Response>;

public class AuthorizationBehavior<TRequest> : IPipelineBehavior<TRequest, Response>
    where TRequest : IAuthorizeableRequest<Response>
{
    public async Task<Response> Handle(TRequest request, RequestHandlerDelegate<Response> next, CancellationToken cancellationToken)
    {
        return await next();
    }
}

public class AuthorizationBehaviorTests
{
    private readonly RequestHandlerDelegate<Response> _mockNextBehavior;
    private readonly AuthorizationBehavior<PlainRequest> _plain = new();
    private readonly AuthorizationBehavior<SecuredRequest> _secured = new();

    public AuthorizationBehaviorTests()
    {
        _mockNextBehavior = Substitute.For<RequestHandlerDelegate<Response>>();
        _mockNextBehavior.Invoke().Returns(Task.FromResult(new Response("ok")));
    }

    public async Task Handle_Plain_CallsNext()
    {
        Response result = await _plain.Handle(new PlainRequest(), _mockNextBehavior, default);

        await _mockNextBehavior.Received(1).Invoke();
    }

    public async Task Handle_Secured_CallsNext()
    {
        Response result = await _secured.Handle(new SecuredRequest("admin"), this._mockNextBehavior, CancellationToken.None);

        await _mockNextBehavior.Received(2).Invoke();
    }
}
