using System.Threading;
using System.Threading.Tasks;
using Mediator;
using NSubstitute;

namespace Proofs;

public record Response(string Value);

public interface IAuthorizeableRequest<T> : IRequest<T>;

public record PlainRequest : IAuthorizeableRequest<Response>;

public record SecuredRequest(string Role) : IAuthorizeableRequest<Response>;

public class AuthorizationBehavior<TRequest> : IPipelineBehavior<TRequest, Response>
    where TRequest : IAuthorizeableRequest<Response>
{
    public async ValueTask<Response> Handle(TRequest request, MessageHandlerDelegate<TRequest, Response> next, CancellationToken cancellationToken)
    {
        return await next(request, cancellationToken);
    }
}

public class AuthorizationBehaviorTests
{
    private readonly MessageHandlerDelegate<IMessage, Response> _mockNextBehavior;
    private readonly AuthorizationBehavior<PlainRequest> _plain = new();
    private readonly AuthorizationBehavior<SecuredRequest> _secured = new();

    public AuthorizationBehaviorTests()
    {
        _mockNextBehavior = Substitute.For<MessageHandlerDelegate<IMessage, Response>>();
        _mockNextBehavior.Invoke(Arg.Any<IMessage>(), Arg.Any<CancellationToken>()).Returns(ValueTask.FromResult(new Response("ok")));
    }

    public async Task Handle_Plain_CallsNext()
    {
        Response result = await _plain.Handle(new PlainRequest(), (message, cancellationToken) => _mockNextBehavior(message, cancellationToken), default);

        await _mockNextBehavior.Received(1).Invoke(Arg.Any<IMessage>(), Arg.Any<CancellationToken>());
    }

    public async Task Handle_Secured_CallsNext()
    {
        Response result = await _secured.Handle(new SecuredRequest("admin"), (message, cancellationToken) => this._mockNextBehavior(message, cancellationToken), CancellationToken.None);

        await _mockNextBehavior.Received(2).Invoke(Arg.Any<IMessage>(), Arg.Any<CancellationToken>());
    }
}
