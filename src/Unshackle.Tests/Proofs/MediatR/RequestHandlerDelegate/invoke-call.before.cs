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
    public async Task Handle_CallsTheDelegateItself()
    {
        var next = Substitute.For<RequestHandlerDelegate<int>>();

        int viaBehavior = await new ValidationBehavior().Handle(new Register("a"), next, default);
        int direct = await next();
    }
}
