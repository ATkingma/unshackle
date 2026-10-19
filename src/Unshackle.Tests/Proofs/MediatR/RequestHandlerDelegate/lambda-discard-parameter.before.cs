using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Proofs;

public record Register(string Name) : IRequest<int>;

public class ValidationBehavior : IPipelineBehavior<Register, int>
{
    public Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token) => next();
}

public class ValidationBehaviorTests
{
    public async Task Handle_LambdaUsesUnderscore()
    {
        RequestHandlerDelegate<int> next = _ => Task.FromResult(_.IsCancellationRequested ? 0 : 1);

        int result = await new ValidationBehavior().Handle(new Register("a"), next, default);
    }
}
