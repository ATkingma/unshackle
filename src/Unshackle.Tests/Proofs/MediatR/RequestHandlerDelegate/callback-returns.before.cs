using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;

namespace Proofs;

public record Register(string Name) : IRequest<int>;

public class ValidationBehavior : IPipelineBehavior<Register, int>
{
    public Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token) => next();
}

public class ValidationBehaviorTests
{
    private readonly RequestHandlerDelegate<int> _next = Substitute.For<RequestHandlerDelegate<int>>();

    public async Task Handle_ReturnsACallback()
    {
        _next.Invoke().Returns(_ => Task.FromResult(3));

        int result = await new ValidationBehavior().Handle(new Register("a"), _next, default);
    }
}
