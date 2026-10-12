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
    public async Task Handle_RealDelegate()
    {
        RequestHandlerDelegate<int> next = _ => Task.FromResult(1);

        int result = await new ValidationBehavior().Handle(new Register("a"), next, default);
    }
}
