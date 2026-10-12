using System.Threading;
using System.Threading.Tasks;
using MediatR;
using NSubstitute;

namespace Proofs;

public record Register(string Name) : IRequest<int>;

public class ValidationBehavior : IPipelineBehavior<Register, int>
{
    public async Task<int> Handle(Register command, RequestHandlerDelegate<int> next, CancellationToken token)
    {
        return command.Name.Length == 0 ? -1 : await next();
    }
}

public class ValidationBehaviorTests
{
    private readonly RequestHandlerDelegate<int> _next = Substitute.For<RequestHandlerDelegate<int>>();
    private readonly ValidationBehavior _behavior = new();

    public async Task Handle_ValidCommand_CallsNext()
    {
        _next.Invoke().Returns(5);

        int result = await _behavior.Handle(new Register("a"), _next, default);

        await _next.Received(1).Invoke();
    }

    public async Task Handle_Local_UsesTaskFromResult()
    {
        var next = Substitute.For<RequestHandlerDelegate<int>>();
        next.Invoke().Returns(Task.FromResult(7));

        int result = await _behavior.Handle(new Register("b"), next, CancellationToken.None);
    }
}
