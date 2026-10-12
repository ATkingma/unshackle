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
    public async Task Handle_HelperTakesTheDelegate(ValidationBehavior behavior)
    {
        await Run(behavior, Substitute.For<RequestHandlerDelegate<int>>());
    }

    private static Task<int> Run(ValidationBehavior behavior, RequestHandlerDelegate<int> next) => behavior.Handle(new Register("a"), next, default);
}
